using backend.Models.Domain;

namespace backend.Services.Interfaces.Cache;

public interface INewsCacheService
{
  Task UpdateNewsGroupsAsync(List<NewsItem> newNewsItems);
  Task<List<NewsItem>> GetNewsAsync(List<DateTime> dates, int newsType);
  Task InvalidateNewsCache();
}