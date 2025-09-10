using backend.Services.Interfaces.NewsAggregation;

namespace backend.Services.Classes.NewsAggregation;

public class NewsCollectorService(
  IGitHubService gitHubService,
  IYouTubeService youTubeService) : INewsCollectorService
{
  public async Task CollectAllNews()
  {
    await gitHubService.GetGitHubAIUpdatesAsync();
    await youTubeService.GetYouTubeAIUpdatesAsync();
  }
}