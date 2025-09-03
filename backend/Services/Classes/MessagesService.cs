using AutoMapper;
using backend.Services.Interfaces;
using backend.Repository.Interfaces;
using backend.Models.Dtos;
using backend.Models.Domain;

using ChatMessage = backend.Models.Domain.ChatMessage;

namespace backend.Services.Classes;

public class MessagesServices(
  ICacheService cacheService,
  IMessagesRepo messagesRepo,
  IMapper mapper) : IMessagesService
{
  public async Task<List<FullMessageDto>> GetStarredMessagesAsync(string userId)
  {
    var cachedMessages = await cacheService.GetStarredMessagesFromCache(userId);
    if (cachedMessages.Any())
    {
      return mapper.Map<List<FullMessageDto>>(cachedMessages);
    }

    // Fallback to database
    var messages = await messagesRepo.GetStarredMessagesAsync(userId);

    return mapper.Map<List<FullMessageDto>>(messages);
  }
  public async Task<(bool IsStarred, ChatMessage? Message)> ToggleStarAsync(Guid messageId, string userId)
  {
    // Update database first
    var message = await messagesRepo.ToggleMessageStarAsync(userId, messageId);

    if (message != null)
    {
      await cacheService.ToggleStarredMessageInCache(userId, message);
      return (message.IsStarred, message);
    }

    return (false, null);
  }
}