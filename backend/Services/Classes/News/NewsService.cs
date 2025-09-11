using backend.Models.Domain;
using backend.Repository.Interfaces;
using backend.Services.Interfaces.News;

namespace backend.Services.Classes.News;

public class NewsService(
  INewsRepo newsRepo,
  ILogger<NewsService> logger
) : INewsService
{
  public async Task<List<NewsItem>> GetNewsItems(int? lastId, int count)
  {
    try
    {
      return await newsRepo.GetNewsAfterAsync(lastId, count);
    }
    catch (Exception ex)
    {
      logger.LogError(ex, "Failed to get news items after ID {LastId}", lastId);
      throw;
    }
  }
}