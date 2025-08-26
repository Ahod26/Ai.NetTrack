using AutoMapper;

public class ChatService
(IChatRepo chatRepo,
IOpenAIService openAIService,
IMapper mapper,
ILLMCacheService LLMCacheService,
ICacheService cacheService) : IChatService
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

    var initialMessage = await GetChatMessagesAsync(chatDto.Id, userId);
    cacheService.SetCachedChat(userId, chatDto.Id, new CachedChatData { Metadata = chatDto, Messages = initialMessage });

    return chatDto;
  }

  public async Task<ChatMetaDataDto?> GetUserChatAsync(Guid chatId, string userId, int? timezoneOffset = null)
  {
    var cachedChat = cacheService.GetCachedChat(userId, chatId);
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
    if (timezoneOffset.HasValue)
    {
      chatDto.CreatedAt = chatDto.CreatedAt.AddMinutes(-timezoneOffset.Value);
      chatDto.LastMessageAt = chatDto.LastMessageAt.AddMinutes(-timezoneOffset.Value);
    }

    var initialMessages = await GetChatMessagesAsync(chatDto.Id, userId);
    cacheService.SetCachedChat(userId, chatDto.Id, new CachedChatData { Metadata = chatDto, Messages = initialMessages });
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
        chatDto.CreatedAt = chatDto.CreatedAt.AddMinutes(-timezoneOffset.Value);
        chatDto.LastMessageAt = chatDto.LastMessageAt.AddMinutes(-timezoneOffset.Value);
      }
    }


    foreach (var metadata in chatDtos)
    {
      var messages = await GetChatMessagesAsync(metadata.Id, userId);
      cacheService.SetCachedChat(userId, metadata.Id, new CachedChatData { Metadata = metadata, Messages = messages });
    }

    return chatDtos;
  }

  public async Task<List<FullMessageDto>> GetAllChatMessagesAsync(Guid chatId, string userId)
  {
    var cachedChat = cacheService.GetCachedChat(userId, chatId);
    if (cachedChat != null)
    {
      return mapper.Map<List<FullMessageDto>>(cachedChat.Messages);
    }
    var messages = await chatRepo.GetMessagesAsync(chatId);
    // Cache messages and metadata on miss
    var chatEntity = await chatRepo.GetChatByIdAndUserIdAsync(chatId, userId);
    if (chatEntity != null)
    {
      var metaDto = mapper.Map<ChatMetaDataDto>(chatEntity);
      cacheService.SetCachedChat(userId, chatId, new CachedChatData { Metadata = metaDto, Messages = messages });
    }
    return mapper.Map<List<FullMessageDto>>(messages);
  }

  public async Task<FullMessageDto> ProcessUserMessageAsync(Guid chatId, string content, string userId, Func<string, Task>? onChunkReceived = null)
  {
    // 1. Save user message
    var userMessage = await AddMessageAsync(chatId, content, MessageType.User, userId);

    // 2. Get chat context (recent messages)
    var context = await GetChatMessagesAsync(chatId, userId);

    // 3. Check cache first (before calling OpenAI)
    var cachedResponse = await LLMCacheService.GetCachedResponseAsync(content, context);
    if (cachedResponse != null)
    {
      // Cache hit - save AI message and return early
      var cachedAiMessage = await AddMessageAsync(chatId, cachedResponse, MessageType.Assistant, userId);
      return cachedAiMessage;
    }

    // 4. Cache miss - Generate AI response (streaming)
    var aiResponse = await openAIService.GenerateResponseAsync(content, context, onChunkReceived);

    // 5. Save AI message (full response)
    var aiMessage = await AddMessageAsync(chatId, aiResponse, MessageType.Assistant, userId);

    if (aiResponse != "Sorry, I'm having trouble responding right now. Please try again.")
    {
      // 6. Cache the response for future use
      await LLMCacheService.SetCachedResponseAsync(content, context, aiResponse);
    }
    
    return aiMessage;
  }

  public async Task DeleteChatByIdAsync(Guid chatId, string userId)
  {
    await chatRepo.DeleteChatAsync(chatId);
    cacheService.DeleteCachedChat(userId, chatId);
  }

  public async Task ChangeChatTitle(Guid chatId, string newTitle, string userId)
  {
    cacheService.ChangeCachedChatTitle(userId, chatId, newTitle);
    await chatRepo.ChangeChatTitleAsync(chatId, newTitle);
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

    var savedMessage = await chatRepo.AddMessageAsync(message);

    // Update chat's last message time
    var chat = await chatRepo.GetChatByIdAsync(chatId);
    if (chat != null)
    {
      chat.LastMessageAt = DateTime.UtcNow;
      chat.MessageCount++;
      await chatRepo.UpdateChatAsync(chat);
    }

    cacheService.AddMessageToCachedChat(userId, chatId, savedMessage);

    var fullMessage = mapper.Map<FullMessageDto>(savedMessage);
    return fullMessage;
  }

  private async Task<List<ChatMessage>> GetChatMessagesAsync(Guid chatId, string userId)
  {
    var cachedChat = cacheService.GetCachedChat(userId, chatId);
    if (cachedChat != null && cachedChat.Messages != null)
    {
      return cachedChat.Messages;
    }
    return await chatRepo.GetMessagesAsync(chatId);
  }
}