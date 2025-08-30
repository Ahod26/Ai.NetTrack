public interface IMessagesRepo
{
  Task<ChatMessage> AddMessageAsync(ChatMessage message);
  Task<List<ChatMessage>> GetMessagesAsync(Guid chatId);
  Task<List<ChatMessage>> GetStarredMessagesAsync(string userId);
  Task<ChatMessage?> ToggleMessageStarAsync(string userId, Guid messageId);
}