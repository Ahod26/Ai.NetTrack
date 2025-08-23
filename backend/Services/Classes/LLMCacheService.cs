using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;
using NRedisStack;
using NRedisStack.RedisStackCommands;
using NRedisStack.Search;
using NRedisStack.Search.Literals.Enums;
using OpenAI.Embeddings;
using StackExchange.Redis;
using static NRedisStack.Search.Schema;

public class LLMCacheService : ILLMCacheService
{
  private readonly IDatabase _database;
  private readonly IDistributedCache _distributedCache;
  private readonly EmbeddingClient _embeddingClient;
  private readonly ILogger<LLMCacheService> _logger;
  private readonly TopicExtractor _topicExtractor;

  private const string EXACT_CACHE_PREFIX = "exact:cache:";
  private const string SEMANTIC_CACHE_PREFIX = "semantic:cache:";
  private const string EMBEDDINGS_INDEX_NAME = "embeddings_idx";
  private const int MAX_CACHEABLE_MESSAGE_COUNT_SEMANTIC = 7;
  private const int MAX_CACHEABLE_MESSAGE_COUNT_EXACT = 2;
  private const float SEMANTIC_SIMILARITY_THRESHOLD = 0.75f; // Realistic threshold 0.7-0.85

  public LLMCacheService(
      IConnectionMultiplexer redis,
      IDistributedCache distributedCache,
      EmbeddingClient embeddingClient,
      ILogger<LLMCacheService> logger)
  {
    _database = redis.GetDatabase();
    _distributedCache = distributedCache;
    _embeddingClient = embeddingClient;
    _logger = logger;
    _topicExtractor = new TopicExtractor();

    // Initialize Redis index on startup
    InitializeRedisIndexAsync().Wait();
  }

  #region Public Interface Methods

  // Main method for exact + semantic
  public async Task SetCachedResponseAsync(string userMessage, List<ChatMessage> context, string response)
  {
    try
    {
      // Skip caching for conversations too long
      if (context.Count > MAX_CACHEABLE_MESSAGE_COUNT_SEMANTIC)
      {
        _logger.LogDebug("Skipping cache for conversation with {MessageCount} messages (max: {MaxCount})", context.Count, MAX_CACHEABLE_MESSAGE_COUNT_SEMANTIC);
        return;
      }
      
      var expiration = CalculateExpiration(context.Count);

      // Exact key cache
      if (context.Count <= MAX_CACHEABLE_MESSAGE_COUNT_EXACT)
      {
        var exactCacheKey = GenerateCacheKey(userMessage, context);

        await _distributedCache.SetStringAsync(EXACT_CACHE_PREFIX + exactCacheKey, response, new DistributedCacheEntryOptions
        {
          AbsoluteExpirationRelativeToNow = expiration
        });
      }

      await SetSemanticCacheAsync(userMessage, context, response, expiration);

      _logger.LogDebug("Cached response for {MessageCount} messages with {ExpirationDays} days expiration", context.Count, expiration.TotalDays);
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Failed to cache LLM response");
    }
  }

  // Getting cached responses (exact + semantic)
  public async Task<string?> GetCachedResponseAsync(string userMessage, List<ChatMessage> context)
  {
    try
    {
      // Skip semantic search for conversations too long
      if (context.Count > MAX_CACHEABLE_MESSAGE_COUNT_SEMANTIC)
      {
        return null;
      }

      // 1. Exact cache first (fastest)
      var exactCacheKey = GenerateCacheKey(userMessage, context);
      var exactResult = await GetExactCachedResponseAsync(exactCacheKey);
      if (exactResult != null)
      {
        _logger.LogDebug("Exact cache hit for key: {CacheKey}", exactCacheKey);
        return exactResult;
      }

      // 2. Try semantic cache (similarity-based)
      var semanticResult = await GetSemanticCacheAsync(userMessage, context);
      if (semanticResult != null)
      {
        _logger.LogDebug("Semantic cache hit with similarity above threshold");
        return semanticResult;
      }

      _logger.LogDebug("Cache miss for {MessageCount} messages", context.Count);
      return null;
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Failed to retrieve cached response");
      return null;
    }
  }

  public async Task ClearAllCacheAsync()
  {
    var server = _database.Multiplexer.GetServer(_database.Multiplexer.GetEndPoints()[0]);

    // Clear exact cache
    var exactKeys = server.Keys(pattern: EXACT_CACHE_PREFIX + "*");
    foreach (var key in exactKeys)
    {
      await _database.KeyDeleteAsync(key);
    }

    // Clear semantic cache
    var semanticKeys = server.Keys(pattern: SEMANTIC_CACHE_PREFIX + "*");
    foreach (var key in semanticKeys)
    {
      await _database.KeyDeleteAsync(key);
    }

    _logger.LogInformation("Cleared all cache entries");
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

  #region Index Management Methods

  // Recreate index
  public async Task RecreateIndexAsync()
  {
    try
    {
      var ft = _database.FT();
      await ft.DropIndexAsync(EMBEDDINGS_INDEX_NAME);
      _logger.LogInformation("Dropped existing index");
    }
    catch (Exception ex)
    {
      _logger.LogDebug("Index didn't exist or couldn't be dropped: {Error}", ex.Message);
    }

    // Wait a moment for cleanup
    await Task.Delay(1000);
    await InitializeRedisIndexAsync();
  }

  // Complete fresh start
  public async Task RefreshIndexAsync()
  {
    try
    {
      // Clear all cache and recreate
      await ClearAllCacheAsync();
      await RecreateIndexAsync();

      _logger.LogInformation("Refreshed index and cleared cache - ready for new data");
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Failed to refresh index");
    }
  }

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

    // Store in Redis as HASH with individual fields
    var semanticKey = SEMANTIC_CACHE_PREFIX + Guid.NewGuid().ToString();

    // Convert embedding to byte array
    var embeddingBytes = embedding.SelectMany(BitConverter.GetBytes).ToArray();

    var hashFields = new HashEntry[]
    {
      new("response", response),
      new("embedding", embeddingBytes),
      new("message_count", context.Count),
      new("topics", string.Join(",", topics)),
      new("cached_at", DateTimeOffset.UtcNow.ToUnixTimeSeconds()), //seconds from january 1 1970 utc 
      new("context_hash", ComputeSha256Hash(fullContext))
    };

    await _database.HashSetAsync(semanticKey, hashFields);
    await _database.KeyExpireAsync(semanticKey, expiration);

    _logger.LogDebug("Stored semantic cache with key: {Key}, embedding length: {Length}, topics: {Topics}",
      semanticKey, embedding.Length, string.Join(",", topics));
  }

  private async Task<string?> GetSemanticCacheAsync(string userMessage, List<ChatMessage> context)
  {
    // Build context and generate embedding
    var fullContext = BuildContextString(userMessage, context);
    var embeddingResponse = await _embeddingClient.GenerateEmbeddingsAsync(new List<string> { fullContext });
    var queryEmbedding = embeddingResponse.Value.First().ToFloats().ToArray();

    // Convert float array to byte array for Redis
    var queryVector = queryEmbedding.SelectMany(BitConverter.GetBytes).ToArray();

    // Search for similar vectors with message count filter
    var ft = _database.FT();
    var query = new Query($"@message_count:[{context.Count} {context.Count}]=>[KNN 1 @embedding $vector]")
        .AddParam("vector", queryVector)
        .SetSortBy("__embedding_score")
        .Dialect(2);

    var results = await ft.SearchAsync(EMBEDDINGS_INDEX_NAME, query);

    // Find results above similarity threshold
    var bestMatch = results.Documents.FirstOrDefault();

    if (bestMatch != null)
    {
      var score = float.Parse(bestMatch["__embedding_score"].ToString());
      var similarity = 1.0f - score;

      if (similarity >= SEMANTIC_SIMILARITY_THRESHOLD)
      {
        var response = await _database.HashGetAsync(bestMatch.Id, "response");
        return response.ToString();
      }
    }
    return null;
  }

  private string GenerateCacheKey(string prompt, List<ChatMessage> context)
  {
    var contextString = BuildContextString(prompt, context);
    return ComputeSha256Hash(contextString);
  }

  private async Task<string?> GetExactCachedResponseAsync(string cacheKey)
  {
    return await _distributedCache.GetStringAsync(EXACT_CACHE_PREFIX + cacheKey);
  }

  private async Task InitializeRedisIndexAsync()
  {
    try
    {
      var ft = _database.FT();

      // Check if index already exists
      try
      {
        var indexInfo = await ft.InfoAsync(EMBEDDINGS_INDEX_NAME);
        _logger.LogDebug("Redis index {IndexName} already exists", EMBEDDINGS_INDEX_NAME);
        return;
      }
      catch
      {
        // Index doesn't exist, create it
      }

      await ft.CreateAsync(EMBEDDINGS_INDEX_NAME, new FTCreateParams()
          .Prefix(SEMANTIC_CACHE_PREFIX),
          new Schema()
              .AddVectorField("embedding", VectorField.VectorAlgo.HNSW, new Dictionary<string, object>
              {
                ["TYPE"] = "FLOAT32",
                ["DIM"] = 1536, // OpenAI text-embedding-3-small dimension
                ["DISTANCE_METRIC"] = "COSINE"
              })
              .AddNumericField("message_count")
              .AddTagField("topics")
              .AddNumericField("cached_at"));

      _logger.LogInformation("Created Redis search index: {IndexName}", EMBEDDINGS_INDEX_NAME);

      // Wait for index to be ready
      await Task.Delay(2000);
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Failed to create Redis search index");
    }
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
    var baseDays = 21; // Base lifetime
    var decayFactor = Math.Pow(0.7, messageCount - 1);
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