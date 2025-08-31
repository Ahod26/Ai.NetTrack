public interface IChatCacheRepo
{
  Task<CachedChatData?> GetCachedChatAsync(string cacheKey);
  Task SetCachedChatAsync(string cacheKey, CachedChatData data, TimeSpan expiration);
  Task UpdateCachedChatAsync(string cacheKey, CachedChatData data, TimeSpan expiration);
  Task DeleteCachedChatAsync(string cacheKey);
  Task<List<ChatMessage>> GetAllStarredMessagesAsync(string userKeyPattern);
  Task UpdateMessageStarStatusAsync(string userKeyPattern, Guid messageId, bool isStarred);
}