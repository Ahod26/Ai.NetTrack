using Microsoft.EntityFrameworkCore;

public class ChatRepo(ApplicationDbContext dbContext) : IChatRepo
{
  public async Task<Chat> CreateChatAsync(Chat chat)
  {
    dbContext.Chats.Add(chat);
    await dbContext.SaveChangesAsync();
    return chat;
  }

  public async Task<Chat?> GetChatByIdAndUserIdAsync(Guid chatId, string userId)
  {
    return await dbContext.Chats
        .FirstOrDefaultAsync(c => c.Id == chatId && c.UserId == userId);
  }

  public async Task<List<Chat>> GetChatsByUserIdAsync(string userId)
  {
    return await dbContext.Chats
        .Where(c => c.UserId == userId)
        .OrderByDescending(c => c.LastMessageAt)
        .ToListAsync();
  }

  public async Task UpdateChatAsync(Chat chat)
  {
    dbContext.Chats.Update(chat);
    await dbContext.SaveChangesAsync();
  }

  public async Task<ChatMessage> AddMessageAsync(ChatMessage message)
  {
    dbContext.ChatMessages.Add(message);
    await dbContext.SaveChangesAsync();
    return message;
  }

  public async Task<List<ChatMessage>> GetMessagesAsync(Guid chatId)
  {
    return await dbContext.ChatMessages
    .Where(m => m.ChatId == chatId)
    .OrderBy(m => m.CreatedAt)  // Chronological order
    .ToListAsync();
  }

  public async Task<Chat?> GetChatByIdAsync(Guid chatId)
  {
    return await dbContext.Chats.FirstOrDefaultAsync((c) => c.Id == chatId);
  }

  public async Task DeleteChatAsync(Guid chatId)
  {
    var chat = await dbContext.Chats.FindAsync(chatId);
    if (chat != null)
    {
      dbContext.Chats.Remove(chat);
      await dbContext.SaveChangesAsync();
    }
  }

  public async Task ChangeChatTitleAsync(Guid chatId, string newTitle)
  {
    var chat = await dbContext.Chats.FindAsync(chatId);

    if (chat != null)
    {
      chat.Title = newTitle;
      await dbContext.SaveChangesAsync();
    }
  }

  public async Task ChangeContextStatus(Guid chatId)
  {
    var chat = await dbContext.Chats.FindAsync(chatId);
    if (chat != null)
    {
      chat.IsContextFull = true;
      await dbContext.SaveChangesAsync();
    }
  }

  public async Task<int> GetUserChatCountAsync(string userId)
  {
    return await dbContext.Chats.CountAsync(c => c.UserId == userId);
  }
}