
using backend.Background.Interfaces;

namespace backend.Background.Classes;
public class NewsCollectorService(
  IGitHubService gitHubService,
  IYouTubeService youTubeService,
  IRssService rssService) : INewsCollectorService
{
  public async Task CollectAllNews()
  {
    await gitHubService.GetGitHubAIUpdatesAsync();
    await youTubeService.GetYouTubeAIUpdatesAsync();
    await rssService.GetRSSUpdatesAsync();
  }
}