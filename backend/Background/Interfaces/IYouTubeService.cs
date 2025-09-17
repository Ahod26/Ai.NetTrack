using backend.Models.Domain;

namespace backend.Background.Interfaces;

public interface IYouTubeService
{
  Task<List<NewsItem>> GetYouTubeAIUpdatesAsync();
}