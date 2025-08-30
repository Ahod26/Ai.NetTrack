using Microsoft.EntityFrameworkCore;

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
    return await dbContext.ChatMessages
    .Where(m => m.ChatId == chatId)
    .OrderBy(m => m.CreatedAt)  // Chronological order
    .ToListAsync();
  }

  public async Task<List<ChatMessage>> GetStarredMessagesAsync(string userId)
  {
    return await dbContext.ChatMessages
    .Where(m => m.IsStarred && m.Chat.UserId == userId)
    .OrderBy(m => m.CreatedAt)
    .ToListAsync();
  }

  public async Task<ChatMessage?> ToggleMessageStarAsync(string userId, Guid messageId)
  {
    var message = await dbContext.ChatMessages
        .FirstOrDefaultAsync(m => m.Id == messageId && m.Chat.UserId == userId);

    if (message != null)
    {
      message.IsStarred = !message.IsStarred;
      await dbContext.SaveChangesAsync();
      return message;
    }
    return null;
  }
}