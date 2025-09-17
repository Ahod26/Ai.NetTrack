using backend.Models.Domain;

namespace backend.Background.Interfaces;

public interface IRssService
{
  Task<List<NewsItem>> GetRSSUpdatesAsync();
}