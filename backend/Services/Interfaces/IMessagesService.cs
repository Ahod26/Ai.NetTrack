public interface IMessagesService
{
  Task<List<FullMessageDto>> GetStarredMessagesAsync(string userId);
  Task<(bool IsStarred, ChatMessage? Message)> ToggleStarAsync(Guid messageId, string userId);
}