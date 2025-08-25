public interface IChatService
{
  Task<ChatMetaDataDto> CreateChatAsync(string userId, string firstMessage, int? timezoneOffset = null);
  Task<ChatMetaDataDto?> GetUserChatAsync(Guid chatId, string userId, int? timezoneOffset = null);
  Task<List<ChatMetaDataDto>> GetUserChatsAsync(string userId, int? timezoneOffset = null);
  Task<List<FullMessageDto>> GetAllChatMessagesAsync(Guid chatId);
  Task<FullMessageDto> ProcessUserMessageAsync(Guid chatId, string content, Func<string, Task>? onChunkReceived = null);
  Task DeleteChatByIdAsync(Guid chatId);
  Task ChangeChatTitle(Guid chatId, string newTitle);
}