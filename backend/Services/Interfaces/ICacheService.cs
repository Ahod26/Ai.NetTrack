public interface IChatCacheService
{
  CachedChatData? GetCachedChat(string userId, Guid chatId);
  void SetCachedChat(string userId, Guid chatId, CachedChatData data);
  void AddMessageToCachedChat(string userId, Guid chatId, ChatMessage messageToAdd);
  void ChangeCachedChatTitle(string userId, Guid chatId, string newTitle);
  void DeleteCachedChat(string userId, Guid chatId);
}