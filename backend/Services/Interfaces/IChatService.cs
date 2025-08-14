public interface IChatService
{
  Task<ChatMetaDataDto> CreateChatAsync(string userId, string? title = null, int? timezoneOffset = null);
  Task<ChatMetaDataDto?> GetUserChatAsync(Guid chatId, string userId, int? timezoneOffset = null);
  Task<List<ChatMetaDataDto>> GetUserChatsAsync(string userId, int? timezoneOffset = null);
  Task<ChatMessage> AddMessageAsync(Guid chatId, string content, MessageType type);
  Task<List<ChatMessage>> GetChatMessagesAsync(Guid chatId);
  Task<ChatMessage> ProcessUserMessageAsync(Guid chatId, string content);
  Task DeleteChatByIdAsync(Guid chatId);
  Task ChangeChatTitle(Guid chatId, string newTitle);
}