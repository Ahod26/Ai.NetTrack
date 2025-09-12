using backend.Models.Domain;

namespace backend.Services.Interfaces.Cache;

public interface INewsCacheService
{
  Task<List<NewsItem>> GetNewsAsync(int count);
  Task UpdateNewsGroupsAsync(List<NewsItem> newNewsItems);
  Task<List<NewsItem>> GetNewsByDateAsync(DateTime date);
  Task InvalidateNewsCache();
}