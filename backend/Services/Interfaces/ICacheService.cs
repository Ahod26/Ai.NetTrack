using backend.Models.Dtos;
using backend.Models.Domain;

namespace backend.Services.Interfaces;

public interface ICacheService
{
  Task<CachedChatData?> GetCachedChat(string userId, Guid chatId);
  Task SetCachedChat(string userId, Guid chatId, CachedChatData data);
  Task AddMessageToCachedChat(string userId, Guid chatId, ChatMessage messageToAdd);
  Task ChangeCachedChatTitle(string userId, Guid chatId, string newTitle);
  Task DeleteCachedChat(string userId, Guid chatId);
  Task ChangeCachedChatContextCountStatus(string userId, Guid chatId);
  Task<List<ChatMessage>> GetStarredMessagesFromCache(string userId);
  Task ToggleStarredMessageInCache(string userId, ChatMessage message);
}