using backend.Models.Domain;
using backend.Repository.Interfaces;
using backend.Services.Interfaces.Cache;
using backend.Services.Interfaces.News;

namespace backend.Services.Classes.News;

public class NewsService(
  INewsItemRepo newsRepo,
  INewsCacheService newsCacheService,
  ILogger<NewsService> logger
) : INewsService
{
  public async Task<List<NewsItem>> GetNewsItems(DateTime targetDates, int newsType)
  {
    try
    {
      var newsList = await newsCacheService.GetNewsAsync(targetDates, newsType);
      if (newsList.Count != 0)
      {
        logger.LogWarning("cache hit");
        return newsList;
      }
      var newsListFromDB = await newsRepo.GetNewsAsync(targetDates, newsType);
      await newsCacheService.UpdateNewsGroupsAsync(newsListFromDB);
      logger.LogWarning("DB hit");
      return newsListFromDB;
    }
    catch (Exception ex)
    {
      logger.LogError(ex, "Failed to get news items");
      throw;
    }
  }
}