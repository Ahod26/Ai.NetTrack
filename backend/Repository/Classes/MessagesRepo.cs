using Microsoft.EntityFrameworkCore;
using backend.Models.Domain;
using backend.Repository.Interfaces;
using backend.Data;
using System.Reflection.Metadata.Ecma335;

namespace backend.Repository.Classes;

public class MessagesRepo(ApplicationDbContext dbContext) : IMessagesRepo
{
  public async Task<ChatMessage> AddMessageAsync(ChatMessage message)
  {
    dbContext.ChatMessages.Add(message);
    await dbContext.SaveChangesAsync();
    return message;
  }

  public async Task<List<ChatMessage>> GetMessagesAsync(Guid chatId)
  {
    return await dbContext.ChatMessages.AsNoTracking()
    .Where(m => m.ChatId == chatId)
    .OrderBy(m => m.CreatedAt)  // Chronological order
    .ToListAsync();
  }

  public async Task<List<ChatMessage>> GetStarredMessagesAsync(string userId)
  {
    return await dbContext.ChatMessages.AsNoTracking()
    .Where(m => m.IsStarred && m.Chat.UserId == userId)
    .OrderBy(m => m.CreatedAt)
    .ToListAsync();
  }

  public async Task<ChatMessage?> ToggleMessageStarAsync(string userId, Guid messageId)
  {
    var rowsAffected = await dbContext.ChatMessages
      .Where(m => m.Id == messageId && m.Chat.UserId == userId)
      .ExecuteUpdateAsync(s => s
        .SetProperty(m => m.IsStarred, m => !m.IsStarred));

    if (rowsAffected == 0)
      return null;

    return await dbContext.ChatMessages
      .AsNoTracking()
      .FirstOrDefaultAsync(m => m.Id == messageId);
  }

  public async Task<ChatMessage?> ReportMessageAsync(string userId, Guid messageId, string reportReason)
  {
    var rowsAffected = await dbContext.ChatMessages.
    Where(m => m.Id == messageId && m.Chat.UserId == userId && !m.IsReported)
    .ExecuteUpdateAsync(s => s
      .SetProperty(m => m.IsReported, true)
      .SetProperty(m => m.ReportedAt, DateTime.UtcNow)
      .SetProperty(m => m.ReportReason, reportReason));

    if (rowsAffected == 0)
      return null;

    return await dbContext.ChatMessages
      .AsNoTracking()
      .FirstOrDefaultAsync(m => m.Id == messageId);
  }
}