using backend.Models.Domain;
using backend.Repository.Interfaces;
using backend.Services.Interfaces.Cache;

namespace backend.Services.Classes.Cache;

public class NewsCacheService(
    INewsCacheRepo newsCacheRepo,
    ILogger<NewsCacheService> logger
) : INewsCacheService
{
  public async Task<List<NewsItem>> GetNewsAsync(int count)
  {
    try
    {
      var allNews = new List<NewsItem>();
      var currentDate = DateTime.UtcNow.Date;
      var daysChecked = 0;

      // Keep going back in time until we have enough news or checked 30 days
      while (allNews.Count < count && daysChecked < 30)
      {
        var dateKey = GenerateDateKey(currentDate);
        var dayNews = await newsCacheRepo.GetNewsByDateAsync(dateKey);

        if (dayNews != null && dayNews.Any())
        {
          allNews.AddRange(dayNews);
        }

        currentDate = currentDate.AddDays(-1);
        daysChecked++;
      }

      // Sort by PublishedDate descending (newest first) and take requested count
      return allNews
          .OrderByDescending(n => n.PublishedDate)
          .Take(count)
          .ToList();
    }
    catch (Exception ex)
    {
      logger.LogError(ex, "Error getting cached news");
      return [];
    }
  }

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
        var existingNews = await newsCacheRepo.GetNewsByDateAsync(dateKey) ?? [];

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

  public async Task<List<NewsItem>> GetNewsByDateAsync(DateTime date)
  {
    try
    {
      var dateKey = GenerateDateKey(date.Date);
      return await newsCacheRepo.GetNewsByDateAsync(dateKey) ?? [];
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