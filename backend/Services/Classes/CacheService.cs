using backend.Configuration;
using Microsoft.Extensions.Options;

public class CacheService(IChatCacheRepo chatCacheRepo,
IOptions<ChatCacheSettings> cacheOptions) : ICacheService
{
  private readonly ChatCacheSettings cacheSettings = cacheOptions.Value;

  public async Task<CachedChatData?> GetCachedChat(string userId, Guid chatId)
  {
    var key = GenerateCacheKey(userId, chatId);
    return await chatCacheRepo.GetCachedChatAsync(key);
  }

  public async Task SetCachedChat(string userId, Guid chatId, CachedChatData data)
  {
    var key = GenerateCacheKey(userId, chatId);
    await chatCacheRepo.SetCachedChatAsync(key, data, cacheSettings.CacheDuration);
  }

  public async Task AddMessageToCachedChat(string userId, Guid chatId, ChatMessage messageToAdd)
  {
    var cacheKey = GenerateCacheKey(userId, chatId);
    var existingChat = await chatCacheRepo.GetCachedChatAsync(cacheKey);

    if (existingChat != null)
    {
      existingChat.Messages!.Add(messageToAdd);
      await chatCacheRepo.UpdateCachedChatAsync(cacheKey, existingChat, cacheSettings.CacheDuration);
    }
  }

  public async Task ChangeCachedChatTitle(string userId, Guid chatId, string newTitle)
  {
    var cacheKey = GenerateCacheKey(userId, chatId);
    var existingChat = await chatCacheRepo.GetCachedChatAsync(cacheKey);

    if (existingChat != null)
    {
      existingChat.Metadata!.Title = newTitle;
      await chatCacheRepo.UpdateCachedChatAsync(cacheKey, existingChat, cacheSettings.CacheDuration);
    }
  }

  public async Task ChangeCachedChatContextCountStatus(string userId, Guid chatId)
  {
    var cacheKey = GenerateCacheKey(userId, chatId);
    var existingChat = await chatCacheRepo.GetCachedChatAsync(cacheKey);

    if (existingChat != null)
    {
      existingChat.Metadata!.IsContextFull = true;
      await chatCacheRepo.UpdateCachedChatAsync(cacheKey, existingChat, cacheSettings.CacheDuration);
    }
  }

  public async Task DeleteCachedChat(string userId, Guid chatId)
  {
    var key = GenerateCacheKey(userId, chatId);
    await chatCacheRepo.DeleteCachedChatAsync(key);
  }

  public async Task<List<ChatMessage>> GetStarredMessagesFromCache(string userId)
  {
    var chatKeyPattern = $"chat:{userId}:*";
    return await chatCacheRepo.GetAllStarredMessagesAsync(chatKeyPattern);
  }

  public async Task ToggleStarredMessageInCache(string userId, ChatMessage message)
  {
    var chatKeyPattern = $"chat:{userId}:*";
    await chatCacheRepo.UpdateMessageStarStatusAsync(chatKeyPattern, message.Id, message.IsStarred);
  }

  private string GenerateCacheKey(string userId, Guid chatId)
  {
    return $"chat:{userId}:{chatId}";
  }
}