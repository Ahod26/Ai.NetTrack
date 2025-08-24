using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Caching.Distributed;
using NRedisStack.RedisStackCommands;
using NRedisStack.Search;
using StackExchange.Redis;
using static NRedisStack.Search.Schema;

public class LLMCacheRepo : ILLMCacheRepo
{
  private readonly IDatabase _database;
  private readonly IDistributedCache _distributedCache;
  private readonly ILogger<LLMCacheRepo> _logger;

  private const string EXACT_CACHE_PREFIX = "exact:cache:";
  private const string SEMANTIC_CACHE_PREFIX = "semantic:cache:";
  private const string EMBEDDINGS_INDEX_NAME = "embeddings_idx";

  public LLMCacheRepo(
    IConnectionMultiplexer redis,
    IDistributedCache distributedCache,
    ILogger<LLMCacheRepo> logger)
  {
    _database = redis.GetDatabase();
    _distributedCache = distributedCache;
    _logger = logger;
  }

  public async Task StoreExactCacheAsync(string cacheKey, string response, TimeSpan expiration)
  {
    await _distributedCache.SetStringAsync(EXACT_CACHE_PREFIX + cacheKey, response, new DistributedCacheEntryOptions
    {
      AbsoluteExpirationRelativeToNow = expiration
    });
  }

  public async Task<string?> GetExactCachedResponseAsync(string cacheKey)
  {
    return await _distributedCache.GetStringAsync(EXACT_CACHE_PREFIX + cacheKey);
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
  }

  public async Task InitializeRedisIndexAsync()
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

  public async Task<(string DocumentId, float Score)?> SearchSemanticCacheAsync(byte[] queryVector, int messageCount)
  {
    var ft = _database.FT();
    var query = new Query($"@message_count:[{messageCount} {messageCount}]=>[KNN 1 @embedding $vector]")
        .AddParam("vector", queryVector)
        .SetSortBy("__embedding_score")
        .Dialect(2);

    var results = await ft.SearchAsync(EMBEDDINGS_INDEX_NAME, query);
    var bestMatch = results.Documents.FirstOrDefault();

    if (bestMatch != null)
    {
      var score = float.Parse(bestMatch["__embedding_score"].ToString());
      return (bestMatch.Id, score);
    }
    return null;
  }

  public async Task<string?> GetResponseFromSemanticMatchAsync(string documentId)
  {
    var response = await _database.HashGetAsync(documentId, "response");
    return response.HasValue ? response.ToString() : null;
  }

  public async Task StoreSemanticCacheAsync(string response, byte[] embeddingBytes, int messageCount, List<string> topics, TimeSpan expiration, string fullContext)
  {
    var semanticKey = SEMANTIC_CACHE_PREFIX + Guid.NewGuid().ToString();

    var hashFields = new HashEntry[]
    {
        new("response", response),
        new("embedding", embeddingBytes),
        new("message_count", messageCount),
        new("topics", string.Join(",", topics)),
        new("cached_at", DateTimeOffset.UtcNow.ToUnixTimeSeconds()),
        new("context_hash", ComputeSha256Hash(fullContext))
    };

    await _database.HashSetAsync(semanticKey, hashFields);
    await _database.KeyExpireAsync(semanticKey, expiration);
  }

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

  private string ComputeSha256Hash(string input)
  {
    var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(input));
    return Convert.ToHexString(bytes).ToLower();
  }
}