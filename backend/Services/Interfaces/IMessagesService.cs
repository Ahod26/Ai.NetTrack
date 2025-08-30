public interface IMessagesService
{
  Task<List<FullMessageDto>> GetStarredMessagesAsync(string userId);
  Task ToggleStarAsync(Guid messageId, string userId);
}