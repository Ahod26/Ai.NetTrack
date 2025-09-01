using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Options;
using NRedisStack;
using NRedisStack.RedisStackCommands;
using NRedisStack.Search;
using OpenAI.Embeddings;
using StackExchange.Redis;
using static NRedisStack.Search.Schema;

public class LLMCacheService : ILLMCacheService
{
  private readonly EmbeddingClient _embeddingClient;
  private readonly ILogger<LLMCacheService> _logger;
  private readonly TopicExtractor _topicExtractor;
  private readonly ILLMCacheRepo _LLMCacheRepo;
  private readonly LLMCacheSettings _settings;

  private const string EXACT_CACHE_PREFIX = "exact:cache:";
  private const string SEMANTIC_CACHE_PREFIX = "semantic:cache:";
  private const string EMBEDDINGS_INDEX_NAME = "embeddings_idx";

  public LLMCacheService(
      EmbeddingClient embeddingClient,
      ILLMCacheRepo LLMCacheRepo,
      IOptions<LLMCacheSettings> options,
      ILogger<LLMCacheService> logger)
  {
    _embeddingClient = embeddingClient;
    _LLMCacheRepo = LLMCacheRepo;
    _settings = options.Value;
    _logger = logger;
    _topicExtractor = new TopicExtractor();

    // Initialize Redis index on startup
    _LLMCacheRepo.InitializeRedisIndexAsync().Wait();
  }

  #region Public Interface Methods

  // Main method for exact + semantic
  public async Task SetCachedResponseAsync(string userMessage, List<ChatMessage> context, string response)
  {
    try
    {
      // Skip caching for conversations too long
      if (context.Count > _settings.MaxCacheableMessageCountSemantic)
      {
        return;
      }

      var expiration = CalculateExpiration(context.Count);

      // Exact key cache
      if (context.Count <= _settings.MaxCacheableMessageCountExact)
      {
        var exactCacheKey = GenerateCacheKey(userMessage, context);

        await _LLMCacheRepo.StoreExactCacheAsync(exactCacheKey, response, expiration);
      }

      await SetSemanticCacheAsync(userMessage, context, response, expiration);
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Error in SetCachedResponseAsync for message {UserMessage}", userMessage);
    }
  }

  // Getting cached responses (exact + semantic)
  public async Task<string?> GetCachedResponseAsync(string userMessage, List<ChatMessage> context)
  {
    try
    {
      // Skip semantic search for conversations too long
      if (context.Count > _settings.MaxCacheableMessageCountSemantic)
      {
        return null;
      }

      // 1. Exact cache first (fastest)
      var exactCacheKey = GenerateCacheKey(userMessage, context);
      var exactResult = await _LLMCacheRepo.GetExactCachedResponseAsync(exactCacheKey);
      if (exactResult != null)
      {
        return exactResult;
      }

      // 2. Try semantic cache 
      var semanticResult = await GetSemanticCacheAsync(userMessage, context);
      if (semanticResult != null)
      {
        return semanticResult;
      }
      return null;
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Error in GetCachedResponseAsync for message {UserMessage}", userMessage);
      return null;
    }
  }

  // Could be used later with MCP redis server for delete integration

  // public async Task InvalidateByTopicsAsync(params string[] topics)
  // {
  //   try
  //   {
  //     var ft = _database.FT();

  //     // Build query to find entries with any of the specified topics
  //     var topicQueries = topics.Select(topic => $"@topics:{{{topic}}}");
  //     var queryString = string.Join("|", topicQueries);

  //     var query = new Query(queryString);
  //     var results = await ft.SearchAsync(EMBEDDINGS_INDEX_NAME, query);

  //     foreach (var doc in results.Documents)
  //     {
  //       await _database.KeyDeleteAsync(doc.Id);
  //       _logger.LogDebug("Invalidated cache entry: {CacheKey}", doc.Id);
  //     }

  //     _logger.LogInformation("Invalidated {Count} cache entries for topics: {Topics}",
  //         results.TotalResults, string.Join(", ", topics));
  //   }
  //   catch (Exception ex)
  //   {
  //     _logger.LogError(ex, "Failed to invalidate cache by topics: {Topics}", string.Join(", ", topics));
  //   }
  // }

  #endregion

  #region Private Implementation Methods

  private async Task SetSemanticCacheAsync(string userMessage, List<ChatMessage> context, string response, TimeSpan expiration)
  {
    // Build full context for embedding
    var fullContext = BuildContextString(userMessage, context);

    // Generate embedding using OpenAI
    var embeddingResponse = await _embeddingClient.GenerateEmbeddingsAsync(new List<string> { fullContext });
    var embedding = embeddingResponse.Value.First().ToFloats().ToArray();

    // Extract topics for cache invalidation
    var topics = _topicExtractor.ExtractTopics(fullContext);

    // Convert embedding to byte array
    var embeddingBytes = embedding.SelectMany(BitConverter.GetBytes).ToArray();

    await _LLMCacheRepo.StoreSemanticCacheAsync(response, embeddingBytes, context.Count, topics, expiration, fullContext);
  }

  private async Task<string?> GetSemanticCacheAsync(string userMessage, List<ChatMessage> context)
  {
    // Build context and generate embedding
    var fullContext = BuildContextString(userMessage, context);
    var embeddingResponse = await _embeddingClient.GenerateEmbeddingsAsync(new List<string> { fullContext });
    var queryEmbedding = embeddingResponse.Value.First().ToFloats().ToArray();

    // Convert float array to byte array for Redis
    var queryVector = queryEmbedding.SelectMany(BitConverter.GetBytes).ToArray();

    var searchResult = await _LLMCacheRepo.SearchSemanticCacheAsync(queryVector, context.Count());

    if (searchResult.HasValue)
    {
      var similarity = 1.0f - searchResult.Value.Score;

      if (similarity >= _settings.SemanticSimilarityThreshold)
      {
        return await _LLMCacheRepo.GetResponseFromSemanticMatchAsync(searchResult.Value.DocumentId);
      }
    }
    return null;
  }

  private string GenerateCacheKey(string prompt, List<ChatMessage> context)
  {
    var contextString = BuildContextString(prompt, context);
    return ComputeSha256Hash(contextString);
  }

  private string BuildContextString(string userMessage, List<ChatMessage> context)
  {
    var sb = new StringBuilder();

    foreach (var message in context)
    {
      var rolePrefix = message.Type == MessageType.User ? "User" : "Assistant";
      sb.AppendLine($"{rolePrefix}: {message.Content}");
    }

    sb.AppendLine($"User: {userMessage}");
    return sb.ToString();
  }

  private string ComputeSha256Hash(string input)
  {
    var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(input));
    return Convert.ToHexString(bytes).ToLower();
  }

  private TimeSpan CalculateExpiration(int messageCount)
  {
    var baseDays = _settings.BaseCacheLifetimeDays; // Base lifetime
    var decayFactor = Math.Pow(_settings.CacheLifetimeDecayFactor, messageCount - 1);
    var lifetimeDays = Math.Max(1, (int)(baseDays * decayFactor));

    return TimeSpan.FromDays(lifetimeDays);

    // Results: 1 - 21 days, 2 - 15, 3 - 10, 4 - 7, 5 - 5, 6,7 - 3,4
  }

  #endregion
}

// Helper classes
public class SemanticCacheEntry
{
  public required string Response { get; set; }
  public required float[] Embedding { get; set; }
  public required int MessageCount { get; set; }
  public required List<string> Topics { get; set; }
  public required DateTimeOffset CachedAt { get; set; }
  public required string ContextHash { get; set; }
}

public class TopicExtractor
{
  private readonly Dictionary<string, string> _techTerms = new()
  {
    ["asp.net"] = "aspnet",
    ["signalr"] = "signalr",
    ["blazor"] = "blazor",
    ["mcp"] = "mcp",
    ["semantic kernel"] = "semantic-kernel",
    ["redis"] = "redis",
    ["openai"] = "openai",
    ["azure"] = "azure",
    ["entity framework"] = "ef",
    ["minimal api"] = "minimal-api",
    [".net"] = "dotnet",
    ["c#"] = "csharp",
    ["visual studio"] = "visualstudio",
    ["nuget"] = "nuget",
    ["maui"] = "maui"
  };

  public List<string> ExtractTopics(string text)
  {
    var lowerText = text.ToLower();
    var foundTopics = new List<string>();

    foreach (var term in _techTerms)
    {
      if (lowerText.Contains(term.Key))
        foundTopics.Add(term.Value);
    }

    return foundTopics.Distinct().ToList();
  }
}