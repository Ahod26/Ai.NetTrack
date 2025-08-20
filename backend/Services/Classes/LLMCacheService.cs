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
  private const int MAX_CACHEABLE_MESSAGE_COUNT = 10;
  private const float SEMANTIC_SIMILARITY_THRESHOLD = 0.85f;

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
    _ = Task.Run(InitializeRedisIndexAsync);
  }

  public async Task SetCachedResponseAsync(string cacheKey, string response, TimeSpan? expiration = null)
  {
    // This overload is for exact caching only
    await _distributedCache.SetStringAsync(EXACT_CACHE_PREFIX + cacheKey, response, new DistributedCacheEntryOptions
    {
      AbsoluteExpirationRelativeToNow = expiration ?? TimeSpan.FromHours(1)
    });
  }

  // Main method for semantic + exact caching
  public async Task SetCachedResponseAsync(string userMessage, List<ChatMessage> context, string response)
  {
    try
    {
      // Skip caching for conversations too long
      if (context.Count > MAX_CACHEABLE_MESSAGE_COUNT)
      {
        _logger.LogDebug("Skipping cache for conversation with {MessageCount} messages (max: {MaxCount})",
            context.Count, MAX_CACHEABLE_MESSAGE_COUNT);
        return;
      }

      // 1. Store exact cache (fast lookup for identical requests)
      var exactCacheKey = GenerateCacheKey(userMessage, context);
      var exactExpiration = CalculateExpiration(context.Count);
      await SetCachedResponseAsync(exactCacheKey, response, exactExpiration);

      // 2. Store semantic cache (similarity-based lookup)
      await SetSemanticCacheAsync(userMessage, context, response, exactExpiration);

      _logger.LogDebug("Cached response for {MessageCount} messages with {ExpirationDays} days expiration",
          context.Count, exactExpiration.TotalDays);
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Failed to cache LLM response");
      // Don't throw - caching failures shouldn't break the application
    }
  }

  public async Task<string?> GetCachedResponseAsync(string cacheKey)
  {
    // Try exact cache first
    return await _distributedCache.GetStringAsync(EXACT_CACHE_PREFIX + cacheKey);
  }

  // Main method for getting cached responses (exact + semantic)
  public async Task<string?> GetCachedResponseAsync(string userMessage, List<ChatMessage> context)
  {
    try
    {
      // Skip semantic search for conversations too long
      if (context.Count > MAX_CACHEABLE_MESSAGE_COUNT)
      {
        return null;
      }

      // 1. Try exact cache first (fastest)
      var exactCacheKey = GenerateCacheKey(userMessage, context);
      var exactResult = await GetCachedResponseAsync(exactCacheKey);
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
      return null; // Don't break the application on cache failures
    }
  }

  public string GenerateCacheKey(string prompt, List<ChatMessage> context)
  {
    var contextString = BuildContextString(prompt, context);
    return ComputeSha256Hash(contextString);
  }

  public async Task RemoveCachedResponseAsync(string cacheKey)
  {
    await _distributedCache.RemoveAsync(EXACT_CACHE_PREFIX + cacheKey);
    // Note: Semantic cache cleanup happens via TTL
  }

  public async Task RemoveByPatternAsync(string pattern)
  {
    // For pattern-based removal, we'll use Redis directly
    var server = _database.Multiplexer.GetServer(_database.Multiplexer.GetEndPoints()[0]);
    var keys = server.Keys(pattern: EXACT_CACHE_PREFIX + pattern + "*");

    foreach (var key in keys)
    {
      await _database.KeyDeleteAsync(key);
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

  // Invalidate cache by topics (for MCP server integration)
  public async Task InvalidateByTopicsAsync(params string[] topics)
  {
    try
    {
      var ft = _database.FT();

      // Build query to find entries with any of the specified topics
      var topicQueries = topics.Select(topic => $"@topics:{{{topic}}}");
      var queryString = string.Join("|", topicQueries);

      var query = new Query(queryString);
      var results = await ft.SearchAsync(EMBEDDINGS_INDEX_NAME, query);

      foreach (var doc in results.Documents)
      {
        await _database.KeyDeleteAsync(doc.Id);
        _logger.LogDebug("Invalidated cache entry: {CacheKey}", doc.Id);
      }

      _logger.LogInformation("Invalidated {Count} cache entries for topics: {Topics}",
          results.TotalResults, string.Join(", ", topics));
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Failed to invalidate cache by topics: {Topics}", string.Join(", ", topics));
    }
  }

  private async Task SetSemanticCacheAsync(string userMessage, List<ChatMessage> context, string response, TimeSpan expiration)
  {
    // Build full context for embedding
    var fullContext = BuildContextString(userMessage, context);

    // Generate embedding using OpenAI
    var embeddingResponse = await _embeddingClient.GenerateEmbeddingsAsync(new List<string> { fullContext });
    var embedding = embeddingResponse.Value.First().ToFloats().ToArray();

    // Extract topics for cache invalidation
    var topics = _topicExtractor.ExtractTopics(fullContext);

    // Create cache entry
    var cacheEntry = new SemanticCacheEntry
    {
      Response = response,
      Embedding = embedding,
      MessageCount = context.Count,
      Topics = topics,
      CachedAt = DateTimeOffset.UtcNow,
      ContextHash = ComputeSha256Hash(fullContext)
    };

    // Store in Redis with semantic search capabilities
    var semanticKey = SEMANTIC_CACHE_PREFIX + Guid.NewGuid().ToString();
    var jsonData = JsonSerializer.Serialize(cacheEntry);

    await _database.HashSetAsync(semanticKey, "data", jsonData);
    await _database.KeyExpireAsync(semanticKey, expiration);
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
    var query = new Query($"@message_count:[{context.Count} {context.Count}]=>[KNN 5 @embedding $vector]")
        .AddParam("vector", queryVector)
        .SetSortBy("__embedding_score")
        .Limit(0, 5)
        .Dialect(2);

    var results = await ft.SearchAsync(EMBEDDINGS_INDEX_NAME, query);

    // Find results above similarity threshold
    foreach (var doc in results.Documents)
    {
      // Get the similarity score
      var scoreProperty = doc.GetProperties().FirstOrDefault(p => p.Key == "__embedding_score");
      if (scoreProperty.Key != null && !scoreProperty.Value.IsNull && float.TryParse(scoreProperty.Value.ToString(), out var score))
      {
        if (score >= SEMANTIC_SIMILARITY_THRESHOLD)
        {
          var dataField = doc["data"].ToString();
          var cacheEntry = JsonSerializer.Deserialize<SemanticCacheEntry>(dataField);

          _logger.LogDebug("Semantic cache hit with score: {Score}", score);
          return cacheEntry?.Response;
        }
      }
    }

    return null;
  }

  private async Task InitializeRedisIndexAsync()
  {
    try
    {
      var ft = _database.FT();

      // Check if index already exists
      var indexInfo = await ft.InfoAsync(EMBEDDINGS_INDEX_NAME);
      _logger.LogDebug("Redis index {IndexName} already exists", EMBEDDINGS_INDEX_NAME);
      return;
    }
    catch
    {
      // Index doesn't exist, create it
    }

    try
    {
      var ft = _database.FT();

      await ft.CreateAsync(EMBEDDINGS_INDEX_NAME, new FTCreateParams()
          .On(IndexDataType.HASH)
          .Prefix(SEMANTIC_CACHE_PREFIX),
          new Schema()
              .AddVectorField("$.embedding", Schema.VectorField.VectorAlgo.HNSW, new Dictionary<string, object>
              {
                ["TYPE"] = "FLOAT32",
                ["DIM"] = 1536, // OpenAI text-embedding-3-small dimension
                ["DISTANCE_METRIC"] = "COSINE"
              })
              .AddNumericField("$.message_count")
              .AddTagField("$.topics")
              .AddNumericField("$.cached_at"));

      _logger.LogInformation("Created Redis search index: {IndexName}", EMBEDDINGS_INDEX_NAME);
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
    // Lifetime decreases as message count increases
    // 1 message: 30 days, 10 messages: 1 day
    var lifetimeDays = Math.Max(1, 31 - (messageCount * 3));
    return TimeSpan.FromDays(lifetimeDays);
  }
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