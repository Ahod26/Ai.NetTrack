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

  public List<ChatMessage> GetStarredMessagesFromCache(string userId)
  {
    var key = GenerateStarredCacheKey(userId);
    if (memoryCache.TryGetValue(key, out List<ChatMessage>? starredMessages))
    {
      return starredMessages ?? new List<ChatMessage>();
    }
    return new List<ChatMessage>();
  }

  public void SetStarredMessagesInCache(string userId, List<ChatMessage> starredMessages)
  {
    var key = GenerateStarredCacheKey(userId);
    memoryCache.Set(key, starredMessages, _cacheOptions);
  }

  public void ToggleStarredMessageInCache(string userId, ChatMessage message)
  {
    if (message.IsStarred)
    {
      AddStarredMessageToCache(userId, message);
    }
    else
    {
      RemoveStarredMessageFromCache(userId, message.Id);
    }
  }
  
  private string GenerateCacheKey(string userId, Guid chatId)
  {
    return $"chat:{userId}:{chatId}";
  }
  private string GenerateStarredCacheKey(string userId)
  {
    return $"starred:{userId}";
  }
  private void AddStarredMessageToCache(string userId, ChatMessage message)
  {
    var key = GenerateStarredCacheKey(userId);
    if (memoryCache.TryGetValue(key, out List<ChatMessage>? existingStarred))
    {
      existingStarred ??= new List<ChatMessage>();
      if (!existingStarred.Any(m => m.Id == message.Id))
      {
        existingStarred.Add(message);
        memoryCache.Set(key, existingStarred, _cacheOptions);
      }
    }
    else
    {
      // No cached starred messages yet, create new list
      memoryCache.Set(key, new List<ChatMessage> { message }, _cacheOptions);
    }
  }
  private void RemoveStarredMessageFromCache(string userId, Guid messageId)
  {
    var key = GenerateStarredCacheKey(userId);
    if (memoryCache.TryGetValue(key, out List<ChatMessage>? existingStarred))
    {
      existingStarred ??= new List<ChatMessage>();
      var messageToRemove = existingStarred.FirstOrDefault(m => m.Id == messageId);
      if (messageToRemove != null)
      {
        existingStarred.Remove(messageToRemove);
        memoryCache.Set(key, existingStarred, _cacheOptions);
      }
    }
  }
}