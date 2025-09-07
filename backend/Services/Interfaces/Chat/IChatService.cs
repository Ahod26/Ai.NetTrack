namespace backend.Services.Interfaces.Chat;

using backend.Models.Dtos;

public interface IChatService
{
  Task<ChatMetaDataDto> CreateChatAsync(string userId, string firstMessage, int? timezoneOffset = null);
  Task<ChatMetaDataDto?> GetUserChatAsync(Guid chatId, string userId, int? timezoneOffset = null);
  Task<List<ChatMetaDataDto>> GetUserChatsMetadataAsync(string userId, int? timezoneOffset = null);
  Task<List<FullMessageDto>> GetAllChatMessagesAsync(Guid chatId, string userId);
  Task<FullMessageDto> ProcessUserMessageAsync(Guid chatId, string content, string userId, CancellationToken cancellationToken, Func<string, Task>? onChunkReceived = null);
  Task DeleteChatByIdAsync(Guid chatId, string userId);
  Task ChangeChatTitle(Guid chatId, string newTitle, string userId);
}