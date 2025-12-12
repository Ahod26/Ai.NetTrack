using backend.Models.Configuration;
using Microsoft.Extensions.Options;
using backend.Repository.Interfaces;
using backend.Models.Dtos;


using ChatMessage = backend.Models.Domain.ChatMessage;
using backend.Services.Interfaces.Cache;

namespace backend.Services.Classes.Cache;

public class ChatCacheService(IChatCacheRepo chatCacheRepo,
IOptions<ChatCacheSettings> cacheOptions) : IChatCacheService
{
  private readonly ChatCacheSettings cacheSettings = cacheOptions.Value;

  public async Task<CachedChatData?> GetCachedChat(string userId, Guid chatId)
  {
    var key = GenerateCacheKey(userId, chatId);
    return await chatCacheRepo.GetCachedChatAsync(key);
  }

  public async Task SetCachedChat(string userId, Guid chatId, CachedChatData data)
  {
    var key = GenerateCacheKey(userId, chatId);
    await chatCacheRepo.SetCachedChatAsync(key, data, cacheSettings.CacheDuration);
  }

  public async Task AddMessageToCachedChat(string userId, Guid chatId, ChatMessage messageToAdd)
  {
    var cacheKey = GenerateCacheKey(userId, chatId);
    var existingChat = await chatCacheRepo.GetCachedChatAsync(cacheKey);

    if (existingChat != null)
    {
      existingChat.Messages!.Add(messageToAdd);
      await chatCacheRepo.UpdateCachedChatAsync(cacheKey, existingChat, cacheSettings.CacheDuration);
    }
  }

  public async Task ChangeCachedChatTitle(string userId, Guid chatId, string newTitle)
  {
    var cacheKey = GenerateCacheKey(userId, chatId);
    var existingChat = await chatCacheRepo.GetCachedChatAsync(cacheKey);

    if (existingChat != null)
    {
      var newMetadata = existingChat.Metadata! with { Title = newTitle };
      var updatedChat = existingChat with { Metadata = newMetadata };
      await chatCacheRepo.UpdateCachedChatAsync(cacheKey, updatedChat, cacheSettings.CacheDuration);
    }
  }

  public async Task ChangeCachedChatContextCountStatus(string userId, Guid chatId)
  {
    var cacheKey = GenerateCacheKey(userId, chatId);
    var existingChat = await chatCacheRepo.GetCachedChatAsync(cacheKey);

    if (existingChat != null)
    {
      var newMetadata = existingChat.Metadata! with { IsContextFull = true };
      var updatedChat = existingChat with { Metadata = newMetadata };
      await chatCacheRepo.UpdateCachedChatAsync(cacheKey, updatedChat, cacheSettings.CacheDuration);
    }
  }

  public async Task DeleteCachedChat(string userId, Guid chatId)
  {
    var key = GenerateCacheKey(userId, chatId);
    await chatCacheRepo.DeleteCachedChatAsync(key);
  }

  public async Task<List<ChatMessage>> GetStarredMessagesFromCache(string userId)
  {
    var chatKeyPattern = $"chat:{userId}:*";
    return await chatCacheRepo.GetAllStarredMessagesAsync(chatKeyPattern);
  }

  public async Task ToggleStarredMessageInCache(string userId, Guid chatId, Guid messageId)
  {
    var cacheKey = GenerateCacheKey(userId, chatId);
    var existingChat = await chatCacheRepo.GetCachedChatAsync(cacheKey);

    if (existingChat != null && existingChat.Messages != null)
    {
      var messageToUpdate = existingChat.Messages.FirstOrDefault(m => m.Id == messageId);
      if (messageToUpdate != null)
        messageToUpdate.IsStarred = !messageToUpdate.IsStarred;

      await chatCacheRepo.UpdateCachedChatAsync(cacheKey, existingChat, cacheSettings.CacheDuration);
    }
  }

  public async Task SetReportedMessage(string userId, ChatMessage message)
  {
    var cacheKey = GenerateCacheKey(userId, message.ChatId);
    var existingChat = await chatCacheRepo.GetCachedChatAsync(cacheKey);

    if (existingChat != null)
    {
      var messageToUpdate = existingChat.Messages?.FirstOrDefault(m => m.Id == message.Id);
      if (messageToUpdate != null)
      {
        messageToUpdate.IsReported = true;
        await chatCacheRepo.UpdateCachedChatAsync(cacheKey, existingChat, cacheSettings.CacheDuration);
      }
    }
  }

  private string GenerateCacheKey(string userId, Guid chatId)
  {
    return $"chat:{userId}:{chatId}";
  }
}