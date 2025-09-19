using backend.Models.Domain;

namespace backend.Services.Interfaces.Cache;

public interface INewsCacheService
{
  Task UpdateNewsGroupsAsync(List<NewsItem> newNewsItems);
  Task<List<NewsItem>> GetNewsAsync(DateTime date, int newsType);
  Task InvalidateNewsCache();
}