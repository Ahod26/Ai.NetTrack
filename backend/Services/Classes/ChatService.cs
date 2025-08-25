using System.Runtime.CompilerServices;
using AutoMapper;

public class ChatService
(IChatRepo chatRepo, IOpenAIService openAIService, IMapper mapper, ILLMCacheService cacheService) : IChatService
{
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
    if (timezoneOffset.HasValue)
    {
      chatDto.CreatedAt = chatDto.CreatedAt.AddMinutes(-timezoneOffset.Value);
      chatDto.LastMessageAt = chatDto.LastMessageAt.AddMinutes(-timezoneOffset.Value);
    }

    return chatDto;
  }

  public async Task<ChatMetaDataDto?> GetUserChatAsync(Guid chatId, string userId, int? timezoneOffset = null)
  {
    var chat = await chatRepo.GetChatByIdAndUserIdAsync(chatId, userId);

    if (chat == null)
    {
      return null;
    }

    var chatDto = mapper.Map<ChatMetaDataDto>(chat);

    // Apply timezone conversion if offset is provided
    if (timezoneOffset.HasValue)
    {
      chatDto.CreatedAt = chatDto.CreatedAt.AddMinutes(-timezoneOffset.Value);
      chatDto.LastMessageAt = chatDto.LastMessageAt.AddMinutes(-timezoneOffset.Value);
    }

    return chatDto;
  }

  public async Task<List<ChatMetaDataDto>> GetUserChatsAsync(string userId, int? timezoneOffset = null)
  {
    var chats = await chatRepo.GetChatsByUserIdAsync(userId);
    var chatDtos = mapper.Map<List<ChatMetaDataDto>>(chats);

    // Apply timezone conversion if offset is provided
    if (timezoneOffset.HasValue)
    {
      foreach (var chatDto in chatDtos)
      {
        chatDto.CreatedAt = chatDto.CreatedAt.AddMinutes(-timezoneOffset.Value);
        chatDto.LastMessageAt = chatDto.LastMessageAt.AddMinutes(-timezoneOffset.Value);
      }
    }

    return chatDtos;
  }

  public async Task<List<FullMessageDto>> GetAllChatMessagesAsync(Guid chatId)
  {
    var messages = await chatRepo.GetMessagesAsync(chatId);
    return mapper.Map<List<FullMessageDto>>(messages);
  }

  public async Task<FullMessageDto> ProcessUserMessageAsync(Guid chatId, string content, Func<string, Task>? onChunkReceived = null)
  {
    // 1. Save user message
    var userMessage = await AddMessageAsync(chatId, content, MessageType.User);

    // 2. Get chat context (recent messages)
    var context = await GetChatMessagesAsync(chatId);

    // 3. Check cache first (before calling OpenAI)
    var cachedResponse = await cacheService.GetCachedResponseAsync(content, context);
    if (cachedResponse != null)
    {
      // Cache hit - save AI message and return early
      var cachedAiMessage = await AddMessageAsync(chatId, cachedResponse, MessageType.Assistant);
      return cachedAiMessage;
    }

    // 4. Cache miss - Generate AI response (streaming)
    var aiResponse = await openAIService.GenerateResponseAsync(content, context, onChunkReceived);

    // 5. Save AI message (full response)
    var aiMessage = await AddMessageAsync(chatId, aiResponse, MessageType.Assistant);

    // 6. Cache the response for future use
    await cacheService.SetCachedResponseAsync(content, context, aiResponse);

    return aiMessage;
  }

  public async Task DeleteChatByIdAsync(Guid chatId)
  {
    await chatRepo.DeleteChatAsync(chatId);
  }

  public async Task ChangeChatTitle(Guid chatId, string newTitle)
  {
    await chatRepo.ChangeChatTitleAsync(chatId, newTitle);
  }

  private async Task<FullMessageDto> AddMessageAsync(Guid chatId, string content, MessageType type)
  {
    var message = new ChatMessage
    {
      Id = Guid.NewGuid(),
      ChatId = chatId,
      Content = content,
      Type = type,
      CreatedAt = DateTime.UtcNow
    };

    var savedMessage = await chatRepo.AddMessageAsync(message);

    // Update chat's last message time
    var chat = await chatRepo.GetChatByIdAsync(chatId);
    if (chat != null)
    {
      chat.LastMessageAt = DateTime.UtcNow;
      chat.MessageCount++;
      await chatRepo.UpdateChatAsync(chat);
    }

    var fullMessage = mapper.Map<FullMessageDto>(savedMessage);
    return fullMessage;
  }

  private async Task<List<ChatMessage>> GetChatMessagesAsync(Guid chatId)
  {
    return await chatRepo.GetMessagesAsync(chatId);
  }
}