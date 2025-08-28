using Microsoft.Extensions.Caching.Memory;

public class CacheService(IMemoryCache memoryCache) : ICacheService
{

  // Chat cache duration settings
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
      return cached;
    }
    return null;
  }

  public void SetCachedChat(string userId, Guid chatId, CachedChatData data)
  {
    var key = GenerateCacheKey(userId, chatId);
    memoryCache.Set(key, data, _cacheOptions);
  }

  public void AddMessageToCachedChat(string userId, Guid chatId, ChatMessage messageToAdd)
  {
    var cacheKey = GenerateCacheKey(userId, chatId);

    if (memoryCache.TryGetValue(cacheKey, out CachedChatData? existingChat))
    {
      existingChat!.Messages!.Add(messageToAdd);
      memoryCache.Set(cacheKey, existingChat, _cacheOptions);
    }
  }

  public void ChangeCachedChatTitle(string userId, Guid chatId, string newTitle)
  {
    var cacheKey = GenerateCacheKey(userId, chatId);

    if (memoryCache.TryGetValue(cacheKey, out CachedChatData? existingChat))
    {
      existingChat!.Metadata!.Title = newTitle;
      memoryCache.Set(cacheKey, existingChat, _cacheOptions);
    }
  }

  public void ChangeCachedChatContextCountStatus(string userId, Guid chatId)
  {
    var cacheKey = GenerateCacheKey(userId, chatId);

    if (memoryCache.TryGetValue(cacheKey, out CachedChatData? existingChat))
    {
      existingChat!.Metadata!.IsContextFull = true;
      memoryCache.Set(cacheKey, existingChat, _cacheOptions);
    }
  }

  public void DeleteCachedChat(string userId, Guid chatId)
  {
    var key = GenerateCacheKey(userId, chatId);
    memoryCache.Remove(key);
  }

  private string GenerateCacheKey(string userId, Guid chatId)
  {
    return $"chat:{userId}:{chatId}";
  }
}