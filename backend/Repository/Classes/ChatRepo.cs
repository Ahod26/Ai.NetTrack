using Microsoft.EntityFrameworkCore;
using backend.Models.Domain;
using backend.Repository.Interfaces;
using backend.Data;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Microsoft.EntityFrameworkCore.ChangeTracking.Internal;

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

  public async Task<Chat?> GetChatByIdAsync(Guid chatId)
  {
    return await dbContext.Chats.FirstOrDefaultAsync((c) => c.Id == chatId);
  }

  public async Task DeleteChatAsync(Guid chatId)
  {
    await dbContext.Chats.Where(c => c.Id == chatId).ExecuteDeleteAsync();
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

  // public async Task<bool> GetChatNewsRelationStatus(Guid chatId)
  // {
  //   var chat = await dbContext.Chats.FirstOrDefaultAsync(c => c.Id == chatId);
  //   if (chat != null)
  //     return chat.isChatRelatedToNewsSource;
  //   return false;
  // }

  // public async Task<string?> GetNewsSourceContent(Guid chatId)
  // {
  //   var chat = await dbContext.Chats.FirstOrDefaultAsync(c => c.Id == chatId);
  //   if (chat != null)
  //     return chat.relatedNewsSourceContent;
  //   return null;
  // }
}