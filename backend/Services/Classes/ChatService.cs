public class ChatService(IChatRepo chatRepo, IOpenAIService openAIService) : IChatService
{
  public async Task<Chat> CreateChatAsync(string userId, string? title = null)
  {
    var chat = new Chat
    {
      Id = Guid.NewGuid(),
      UserId = userId,
      Title = title ?? "New Chat",
      CreatedAt = DateTime.UtcNow,
      LastMessageAt = DateTime.UtcNow
    };

    return await chatRepo.CreateChatAsync(chat);
  }

  public async Task<Chat?> GetUserChatAsync(Guid chatId, string userId)
  {
    return await chatRepo.GetChatByIdAndUserIdAsync(chatId, userId);
  }

  public async Task<List<Chat>> GetUserChatsAsync(string userId)
  {
    return await chatRepo.GetChatByUserIdAsync(userId);
  }

  public async Task<ChatMessage> AddMessageAsync(Guid chatId, string content, MessageType type)
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

    return savedMessage;
  }

  public async Task<List<ChatMessage>> GetChatMessagesAsync(Guid chatId)
  {
    return await chatRepo.GetMessagesAsync(chatId);
  }

  public async Task<ChatMessage> ProcessUserMessageAsync(Guid chatId, string content)
  {
    // 1. Save user message
    var userMessage = await AddMessageAsync(chatId, content, MessageType.User);

    // 2. Get chat context (recent messages)
    var context = await GetChatMessagesAsync(chatId);

    // 3. Generate AI response
    var aiResponse = await openAIService.GenerateResponseAsync(content, context);

    // 4. Save AI message
    var aiMessage = await AddMessageAsync(chatId, aiResponse, MessageType.Assistant);

    return aiMessage;
  }

  public async Task DeleteChatByIdAsync(Guid chatId)
  {
    await chatRepo.DeleteChatAsync(chatId);
  }
}