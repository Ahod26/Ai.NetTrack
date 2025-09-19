using backend.Models.Domain;
using backend.Repository.Interfaces;
using backend.Services.Interfaces.Cache;

namespace backend.Services.Classes.Cache;

public class NewsCacheService(
    INewsCacheRepo newsCacheRepo,
    ILogger<NewsCacheService> logger
) : INewsCacheService
{
  public async Task UpdateNewsGroupsAsync(List<NewsItem> newNewsItems)
  {
    try
    {
      // Group news items by their published date
      var newsByDate = newNewsItems
        .Where(n => n.PublishedDate.HasValue)
        .GroupBy(n => n.PublishedDate!.Value.Date)
        .ToList();

      foreach (var dateGroup in newsByDate)
      {
        var dateKey = GenerateDateKey(dateGroup.Key);

        // Get existing news for this date
        var existingNews = await newsCacheRepo.GetNewsAsync(dateKey, 0) ?? [];

        // Add new items to existing ones
        var allNewsForDate = existingNews.ToList();
        allNewsForDate.AddRange(dateGroup.ToList());

        // Remove duplicates based on URL (in case of re-processing)
        var uniqueNews = allNewsForDate
            .GroupBy(n => n.Url)
            .Select(g => g.First())
            .OrderByDescending(n => n.PublishedDate)
            .ToList();

        // Save updated news for this date
        await newsCacheRepo.SetNewsByDateAsync(dateKey, uniqueNews);

        logger.LogInformation("Updated cache for date {Date} with {Count} unique news items",
            dateGroup.Key, uniqueNews.Count);
      }
    }
    catch (Exception ex)
    {
      logger.LogError(ex, "Error updating news groups by date");
      throw;
    }
  }

  public async Task<List<NewsItem>> GetNewsAsync(DateTime date, int newsType)
  {
    try
    {
      var dateKey = GenerateDateKey(date.Date);
      return await newsCacheRepo.GetNewsAsync(dateKey, newsType) ?? [];
    }
    catch (Exception ex)
    {
      logger.LogError(ex, "Error getting news for date {Date}", date.Date);
      return [];
    }
  }

  public async Task InvalidateNewsCache()
  {
    try
    {
      await newsCacheRepo.ClearAllNewsCacheAsync();
      logger.LogInformation("News cache invalidated");
    }
    catch (Exception ex)
    {
      logger.LogError(ex, "Error invalidating news cache");
      throw;
    }
  }

  private static string GenerateDateKey(DateTime date)
  {
    return $"news:date:{date:yyyy-MM-dd}";
  }
}