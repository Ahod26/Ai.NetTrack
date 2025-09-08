using backend.Models.Domain;

namespace backend.Services.Interfaces.NewsAggregation;

public interface IGitHubService
{
  Task<List<NewsItem>> GetGitHubAIUpdatesAsync();
}