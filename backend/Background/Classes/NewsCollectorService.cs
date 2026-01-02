
using backend.Background.Interfaces;

namespace backend.Background.Classes;
public class NewsCollectorService(
  IGitHubService gitHubService,
  IYouTubeService youTubeService,
  IRssService rssService) : INewsCollectorService
{
  public async Task<int> CollectAllNews()
  {
    //var gitHubCollectedNews = await gitHubService.GetGitHubAIUpdatesAsync();
   // var youtubeCollectedNews = await youTubeService.GetYouTubeAIUpdatesAsync();
    //var rssCollectedNews = await rssService.GetRSSUpdatesAsync();

    return 0;
      //rssCollectedNews.Count() + youtubeCollectedNews.Count() + gitHubCollectedNews.Count();
  }
}