using backend.Models.Domain;

namespace backend.Services.Interfaces.NewsAggregation;

public interface IRssService
{
  Task<List<NewsItem>> GetRSSUpdatesAsync();
}