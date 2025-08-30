using AutoMapper;

public class MessagesServices(
  ICacheService cacheService,
  IMessagesRepo messagesRepo,
  IMapper mapper) : IMessagesService
{
  public async Task<List<FullMessageDto>> GetStarredMessagesAsync(string userId)
  {
    var cachedMessages = cacheService.GetStarredMessagesFromCache(userId);
    if (cachedMessages.Any())
    {
      return mapper.Map<List<FullMessageDto>>(cachedMessages);
    }

    // Fallback to database
    var messages = await messagesRepo.GetStarredMessagesAsync(userId);

    if (messages.Any())
    {
      cacheService.SetStarredMessagesInCache(userId, messages);
    }

    return mapper.Map<List<FullMessageDto>>(messages);
  }
  public async Task ToggleStarAsync(Guid messageId, string userId)
  {
    // Update database first
    var message = await messagesRepo.ToggleMessageStarAsync(userId, messageId);

    if (message != null)
    {
      cacheService.ToggleStarredMessageInCache(userId, message);
    }
  }
}