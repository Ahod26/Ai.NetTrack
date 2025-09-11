using backend.Models.Domain;

namespace backend.Repository.Interfaces;

public interface INewsCacheRepo
{
  Task DeleteNewsAsync(string cacheKey);
  Task SetNewsAsync(string cacheKey, List<NewsItem> news, TimeSpan expiration);
  Task<List<NewsItem>?> GetNewsAsync(string cachedKey);
  Task<int> GetLatestGroupNumberAsync();
  Task SetLatestGroupNumberAsync(int groupNumber);
}