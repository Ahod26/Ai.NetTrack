public interface IChatService
{
  Task<Chat> CreateChatAsync(string userId, string? title = null);
  Task<Chat?> GetUserChatAsync(Guid chatId, string userId);
  Task<List<Chat>> GetUserChatsAsync(string userId);
  Task<ChatMessage> AddMessageAsync(Guid chatId, string content, MessageType type);
  Task<List<ChatMessage>> GetChatMessagesAsync(Guid chatId);
  Task<ChatMessage> ProcessUserMessageAsync(Guid chatId, string content);
}