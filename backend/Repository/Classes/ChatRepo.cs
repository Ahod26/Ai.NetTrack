using Microsoft.EntityFrameworkCore;
using backend.Models.Domain;
using backend.Repository.Interfaces;
using backend.Data;


namespace backend.Repository.Classes;

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
    return await dbContext.Chats.AsNoTracking()
        .FirstOrDefaultAsync(c => c.Id == chatId && c.UserId == userId);
  }

  public async Task<List<Chat>> GetChatsByUserIdAsync(string userId)
  {
    return await dbContext.Chats.AsNoTracking()
        .Where(c => c.UserId == userId)
        .OrderByDescending(c => c.LastMessageAt)
        .ToListAsync();
  }

  public async Task UpdateChatMessageCountAndLastMessageAsync(Guid chatId)
  {
    await dbContext.Chats
        .Where(c => c.Id == chatId)
        .ExecuteUpdateAsync(s => s
          .SetProperty(c => c.LastMessageAt, DateTime.UtcNow)
          .SetProperty(c => c.MessageCount, c => c.MessageCount + 1));
  }

  public async Task DeleteChatAsync(Guid chatId)
  {
    await dbContext.Chats.Where(c => c.Id == chatId).ExecuteDeleteAsync();
  }

  public async Task ChangeChatTitleAsync(Guid chatId, string newTitle)
  {
    await dbContext.Chats
      .Where(c => c.Id == chatId)
      .ExecuteUpdateAsync(s => s
        .SetProperty(c => c.Title, newTitle));
  }

  public async Task ChangeContextStatus(Guid chatId)
  {
    await dbContext.Chats
      .Where(c => c.Id == chatId)
      .ExecuteUpdateAsync(s => s
        .SetProperty(c => c.IsContextFull, true));
  }

  public async Task<int> GetUserChatCountAsync(string userId)
  {
    return await dbContext.Chats.AsNoTracking().CountAsync(c => c.UserId == userId);
  }

}