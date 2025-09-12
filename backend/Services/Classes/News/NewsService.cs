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
  public async Task<List<NewsItem>> GetNewsItems(DateTime targetDate)
  {
    try
    {
      var newsList = await newsCacheService.GetNewsByDateAsync(targetDate);
      if (newsList.Count != 0)
      {
        return newsList;
      }
      var newsListFromDB = await newsRepo.GetNewsByDateAsync(targetDate);
      await newsCacheService.UpdateNewsGroupsAsync(newsListFromDB);
      return newsListFromDB;
    }
    catch (Exception ex)
    {
      logger.LogError(ex, "Failed to get news items");
      throw;
    }
  }
}