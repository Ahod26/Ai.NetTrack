using backend.Models.Domain;

namespace backend.Services.Interfaces.NewsAggregation;

public interface IYouTubeService
{
  Task<List<NewsItem>> GetYouTubeAIUpdatesAsync();
}