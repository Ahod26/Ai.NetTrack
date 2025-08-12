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

  public async Task<List<Chat>> GetChatByUserIdAsync(string userId)
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

  public async Task<List<ChatMessage>> GetMessagesAsync(Guid chatId, int count = 50)
  {
    //get the most recent messages, then reverse them back for chronological char order
    return await dbContext.ChatMessages
        .Where(m => m.ChatId == chatId)
        .OrderByDescending(m => m.CreatedAt)
        .Take(count)
        .OrderBy(m => m.CreatedAt)
        .ToListAsync();
  }

  public async Task<Chat?> GetChatByIdAsync(Guid chatId)
  {
    return await dbContext.Chats.FirstOrDefaultAsync((c) => c.Id == chatId);
  }
}