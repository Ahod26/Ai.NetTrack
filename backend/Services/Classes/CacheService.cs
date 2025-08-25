using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using System.Reflection;
using System.Collections.Concurrent;

public class CacheService : ICacheService
{
  private readonly IMemoryCache memoryCache;
  private readonly ILogger<CacheService> logger;

  public CacheService(IMemoryCache memoryCache, ILogger<CacheService> logger)
  {
    this.memoryCache = memoryCache;
    this.logger = logger;
  }
  // Cache duration settings
  private static readonly TimeSpan CacheDuration = TimeSpan.FromHours(2);
  private static readonly TimeSpan SlidingExpiration = TimeSpan.FromMinutes(30);
  private readonly MemoryCacheEntryOptions _cacheOptions = new MemoryCacheEntryOptions()
    .SetAbsoluteExpiration(CacheDuration)
    .SetSlidingExpiration(SlidingExpiration);

  public CachedChatData? GetCachedChat(string userId, Guid chatId)
  {
    var key = GenerateCacheKey(userId, chatId);
    if (memoryCache.TryGetValue(key, out CachedChatData? cached))
    {
      logger.LogWarning("Cache hit for user {UserId}, chat {ChatId}", userId, chatId);
      return cached;
    }
    logger.LogWarning("Cache miss for user {UserId}, chat {ChatId}", userId, chatId);
    return null;
  }

  public void SetCachedChat(string userId, Guid chatId, CachedChatData data)
  {
    var key = GenerateCacheKey(userId, chatId);
    memoryCache.Set(key, data, _cacheOptions);
    logger.LogWarning("Stored chat {ChatId} in cache for user {UserId}", chatId, userId);
  }

  public void AddMessageToCachedChat(string userId, Guid chatId, ChatMessage messageToAdd)
  {
    var cacheKey = GenerateCacheKey(userId, chatId);

    if (memoryCache.TryGetValue(cacheKey, out CachedChatData? existingChat))
    {
      existingChat!.Messages!.Add(messageToAdd);
      memoryCache.Set(cacheKey, existingChat, _cacheOptions);
      logger.LogWarning("Appended message to cached chat {ChatId} for user {UserId}", chatId, userId);
    }
    else
    {
      logger.LogWarning("Cache miss on append for user {UserId}, chat {ChatId}", userId, chatId);
    }
  }

  public void ChangeCachedChatTitle(string userId, Guid chatId, string newTitle)
  {
    var cacheKey = GenerateCacheKey(userId, chatId);

    if (memoryCache.TryGetValue(cacheKey, out CachedChatData? existingChat))
    {
      existingChat!.Metadata!.Title = newTitle;
      memoryCache.Set(cacheKey, existingChat, _cacheOptions);
      logger.LogWarning("Updated title of cached chat {ChatId} for user {UserId}", chatId, userId);
    }
    else
    {
      logger.LogWarning("Cache miss on title change for user {UserId}, chat {ChatId}", userId, chatId);
    }
  }

  public void DeleteCachedChat(string userId, Guid chatId)
  {
    var key = GenerateCacheKey(userId, chatId);
    memoryCache.Remove(key);
    logger.LogWarning("Removed chat {ChatId} from cache for user {UserId}", chatId, userId);
  }

  private string GenerateCacheKey(string userId, Guid chatId)
  {
    return $"chat:{userId}:{chatId}";
  }
}