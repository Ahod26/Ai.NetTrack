using backend.Models.Domain;

namespace backend.Background.Interfaces;

public interface IGitHubService
{
  Task<List<NewsItem>> GetGitHubAIUpdatesAsync();
}