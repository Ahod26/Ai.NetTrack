using backend.Services.Interfaces.NewsAggregation;

namespace backend.Services.Classes.NewsAggregation;

public class NewsCollectorService(
  IGitHubService gitHubService,
  IYouTubeService youTubeService,
  IDocsService docsService,
  IRssService rssService) : INewsCollectorService
{
  public async Task CollectAllNews()
  {
    await gitHubService.GetGitHubAIUpdatesAsync();
    await youTubeService.GetYouTubeAIUpdatesAsync();
    await docsService.GetMicrosoftDocsUpdatesAsync();
    await rssService.GetRSSUpdatesAsync();
  }
}