using AutoMapper;
using backend.Models.Configuration;
using backend.Models.Dtos;
using backend.Services.Interfaces;
using backend.Repository.Interfaces;
using backend.Models.Domain;
using Microsoft.Extensions.Options;

namespace backend.Services.Classes;

public class ChatService(
    IChatRepo chatRepo,
    IMessagesRepo messagesRepo,
    IOpenAIService openAIService,
    IMapper mapper,
    ILLMCacheService LLMCacheService,
    ICacheService cacheService,
    IOptions<StreamingSettings> streamingOptions) : IChatService
{
  private readonly StreamingSettings streamingSettings = streamingOptions.Value;

  public async Task<ChatMetaDataDto> CreateChatAsync(string userId, string firstMessage, int? timezoneOffset = null)
  {
    string title = await openAIService.GenerateChatTitle(firstMessage);

    var chat = new Chat
    {
      Id = Guid.NewGuid(),
      UserId = userId,
      Title = title,
      CreatedAt = DateTime.UtcNow,
      LastMessageAt = DateTime.UtcNow
    };

    var chatCreated = await chatRepo.CreateChatAsync(chat);
    var chatDto = mapper.Map<ChatMetaDataDto>(chatCreated);

    // Apply timezone conversion if offset is provided
    ApplyTimezoneOffset(chatDto, timezoneOffset);

    var initialMessage = await GetChatMessagesAsync(chatDto.Id, userId);
    await cacheService.SetCachedChat(userId, chatDto.Id, new CachedChatData { Metadata = chatDto, Messages = initialMessage });

    return chatDto;
  }

  public async Task<ChatMetaDataDto?> GetUserChatAsync(Guid chatId, string userId, int? timezoneOffset = null)
  {
    var cachedChat = await cacheService.GetCachedChat(userId, chatId);
    if (cachedChat != null)
    {
      return cachedChat.Metadata;
    }

    var chat = await chatRepo.GetChatByIdAndUserIdAsync(chatId, userId);

    if (chat == null)
    {
      return null;
    }

    var chatDto = mapper.Map<ChatMetaDataDto>(chat);

    // Apply timezone conversion if offset is provided
    ApplyTimezoneOffset(chatDto, timezoneOffset);

    var initialMessages = await GetChatMessagesAsync(chatDto.Id, userId);
    await cacheService.SetCachedChat(userId, chatDto.Id, new CachedChatData { Metadata = chatDto, Messages = initialMessages });
    return chatDto;
  }

  public async Task<List<ChatMetaDataDto>> GetUserChatsMetadataAsync(string userId, int? timezoneOffset = null)
  {
    var chats = await chatRepo.GetChatsByUserIdAsync(userId);
    var chatDtos = mapper.Map<List<ChatMetaDataDto>>(chats);

    // Apply timezone conversion if offset is provided
    if (timezoneOffset.HasValue)
    {
      foreach (var chatDto in chatDtos)
      {
        ApplyTimezoneOffset(chatDto, timezoneOffset);
      }
    }

    return chatDtos;
  }

  public async Task<List<FullMessageDto>> GetAllChatMessagesAsync(Guid chatId, string userId)
  {
    var cachedChat = await cacheService.GetCachedChat(userId, chatId);
    if (cachedChat != null)
    {
      return mapper.Map<List<FullMessageDto>>(cachedChat.Messages);
    }
    var messages = await messagesRepo.GetMessagesAsync(chatId);
    // Cache messages and metadata on miss
    var chatEntity = await chatRepo.GetChatByIdAndUserIdAsync(chatId, userId);
    if (chatEntity != null)
    {
      var metaDto = mapper.Map<ChatMetaDataDto>(chatEntity);
      await cacheService.SetCachedChat(userId, chatId, new CachedChatData { Metadata = metaDto, Messages = messages });
    }
    return mapper.Map<List<FullMessageDto>>(messages);
  }

  public async Task<FullMessageDto> ProcessUserMessageAsync(Guid chatId, string content, string userId, CancellationToken cancellationToken, Func<string, Task>? onChunkReceived = null)
  {
    // 1. Get chat context
    var contextFromCache = await GetChatMessagesAsync(chatId, userId);
    // I should make copy for the context so i will work with the original context without the user message or the ai message. I get REFERENCE for the cached item
    var context = new List<ChatMessage>(contextFromCache);

    // 2. Check cache 
    var cachedResponse = await LLMCacheService.GetCachedResponseAsync(content, context);
    if (cachedResponse != null)
    {
      await SimulateStreamingAsync(cachedResponse, onChunkReceived, cancellationToken);
      await AddMessageAsync(chatId, content, MessageType.User, userId);
      var cachedAiMessage = await AddMessageAsync(chatId, cachedResponse, MessageType.Assistant, userId);
      return cachedAiMessage;
    }

    // 3. Cache miss - save user message first
    var userMessage = await AddMessageAsync(chatId, content, MessageType.User, userId);

    // 4. Generate AI response using the original context (before user message was added)
    var aiResponse = await openAIService.GenerateResponseAsync(content, context, cancellationToken, onChunkReceived);

    // 5. Save AI message
    var aiMessage = await AddMessageAsync(chatId, aiResponse.response, MessageType.Assistant, userId);

    // 6. Cache the response using the original context (conversation history before user message)
    if (!cancellationToken.IsCancellationRequested &&
        aiResponse.response != "Sorry, I'm having trouble responding right now. Please try again.")
    {
      await LLMCacheService.SetCachedResponseAsync(content, context, aiResponse.response);
      if (aiResponse.totalTokenUsed >= 50000)
      {
        await ChangeContextStatus(chatId, userId);
      }
    }

    return aiMessage;
  }

  public async Task DeleteChatByIdAsync(Guid chatId, string userId)
  {
    await chatRepo.DeleteChatAsync(chatId);
    await cacheService.DeleteCachedChat(userId, chatId);
  }

  public async Task ChangeChatTitle(Guid chatId, string newTitle, string userId)
  {
    await cacheService.ChangeCachedChatTitle(userId, chatId, newTitle);
    await chatRepo.ChangeChatTitleAsync(chatId, newTitle);
  }

  private async Task ChangeContextStatus(Guid chatId, string userId)
  {
    await cacheService.ChangeCachedChatContextCountStatus(userId, chatId);
    await chatRepo.ChangeContextStatus(chatId);
  }

  private async Task<FullMessageDto> AddMessageAsync(Guid chatId, string content, MessageType type, string userId)
  {
    var message = new ChatMessage
    {
      Id = Guid.NewGuid(),
      ChatId = chatId,
      Content = content,
      Type = type,
      CreatedAt = DateTime.UtcNow
    };

    var savedMessage = await messagesRepo.AddMessageAsync(message);

    // Update chat's last message time
    var chat = await chatRepo.GetChatByIdAsync(chatId);
    if (chat != null)
    {
      chat.LastMessageAt = DateTime.UtcNow;
      chat.MessageCount++;
      await chatRepo.UpdateChatAsync(chat);
    }

    await cacheService.AddMessageToCachedChat(userId, chatId, savedMessage);

    var fullMessage = mapper.Map<FullMessageDto>(savedMessage);
    return fullMessage;
  }

  private async Task<List<ChatMessage>> GetChatMessagesAsync(Guid chatId, string userId)
  {
    var cachedChat = await cacheService.GetCachedChat(userId, chatId);
    if (cachedChat != null && cachedChat.Messages != null)
    {
      return cachedChat.Messages;
    }
    return await messagesRepo.GetMessagesAsync(chatId);
  }

  private void ApplyTimezoneOffset(ChatMetaDataDto chatDto, int? timezoneOffset)
  {
    if (timezoneOffset.HasValue)
    {
      chatDto.CreatedAt = chatDto.CreatedAt.AddMinutes(-timezoneOffset.Value);
      chatDto.LastMessageAt = chatDto.LastMessageAt.AddMinutes(-timezoneOffset.Value);
    }
  }

  private async Task<string> SimulateStreamingAsync(string fullResponse, Func<string, Task>? onChunkReceived, CancellationToken cancellationToken)
  {
    if (onChunkReceived == null)
    {
      return fullResponse;
    }

    var chunkSize = streamingSettings.ChunkSize; // Configurable words per chunk 
    var delayMs = streamingSettings.DelayMs;     // Configurable delay between chunks 

    var words = fullResponse.Split(' ', StringSplitOptions.RemoveEmptyEntries);
    var chunks = new List<string>();

    for (int i = 0; i < words.Length; i += chunkSize)
    {
      var chunk = string.Join(" ", words.Skip(i).Take(chunkSize));

      // Add space after chunk unless it's the last one
      if (i + chunkSize < words.Length)
      {
        chunk += " ";
      }

      chunks.Add(chunk);
    }

    foreach (var chunk in chunks)
    {
      cancellationToken.ThrowIfCancellationRequested();

      await onChunkReceived(chunk);

      // Small delay to simulate real streaming
      if (delayMs > 0)
      {
        await Task.Delay(delayMs, cancellationToken);
      }
    }

    return fullResponse;
  }
}