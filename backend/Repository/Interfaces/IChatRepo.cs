public interface IChatRepo
{
  Task<Chat> CreateChatAsync(Chat chat);
  Task<Chat?> GetChatByIdAndUserIdAsync(Guid chatId, string userId);
  Task<List<Chat>> GetChatByUserIdAsync(string userId);
  Task UpdateChatAsync(Chat chat);
  Task<ChatMessage> AddMessageAsync(ChatMessage message);
  Task<List<ChatMessage>> GetMessagesAsync(Guid chatId, int count = 50);
  Task<Chat?> GetChatByIdAsync(Guid chatId);
}