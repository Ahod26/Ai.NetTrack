using backend.Services.Interfaces.NewsAggregation;

namespace backend.Services.Classes.NewsAggregation;

public class NewsCollectorService(IGitHubService gitHubService) : INewsCollectorService
{
  public async Task CollectAllNews()
  {
    await gitHubService.GetGitHubAIUpdatesAsync();
  }
}