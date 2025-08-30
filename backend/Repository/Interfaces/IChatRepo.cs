public interface IChatRepo
{
  Task<Chat> CreateChatAsync(Chat chat);
  Task<Chat?> GetChatByIdAndUserIdAsync(Guid chatId, string userId);
  Task<List<Chat>> GetChatsByUserIdAsync(string userId);
  Task UpdateChatAsync(Chat chat);
  Task<Chat?> GetChatByIdAsync(Guid chatId);
  Task DeleteChatAsync(Guid chatId);
  Task ChangeChatTitleAsync(Guid chatId, string newTitle);
  Task ChangeContextStatus(Guid chatId);
  Task<int> GetUserChatCountAsync(string userId);
}