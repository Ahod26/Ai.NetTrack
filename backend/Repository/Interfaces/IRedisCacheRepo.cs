using backend.Models.Domain;
using backend.Models.Dtos;

namespace backend.Repository.Interfaces;

public interface IRedisCacheRepo
{
  Task<List<NewsItem>?> GetNewsAsync(string dateKey, int newsType);
  Task SetNewsByDateAsync(string dateKey, List<NewsItem> news);
  Task ClearAllNewsCacheAsync();
  Task AddUserToNewsletterListAsync(string cacheKey, EmailNewsletterDTO user);
  Task RemoveUserFromNewsletterListAsync(string cacheKey, string email);
  Task<List<EmailNewsletterDTO>> GetNewsletterSubscribersListAsync(string cacheKey);
  Task<bool> IsUserInNewsletterListAsync(string cacheKey, string email);
}