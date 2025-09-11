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
      // Get the current highest group number
      var currentGroupNumber = await newsCacheRepo.GetLatestGroupNumberAsync();

      foreach (var newsItem in newNewsItems)
      {
        // Get current group
        var currentGroupKey = $"news:{currentGroupNumber}";
        var currentGroup = await newsCacheRepo.GetNewsAsync(currentGroupKey) ?? [];

        // Check if current group is full (10 items)
        if (currentGroup.Count >= 10)
        {
          // Create new group
          currentGroupNumber++;
          currentGroupKey = $"news:{currentGroupNumber}";
          currentGroup = [];
        }

        // Add news item to current group (insert at beginning for latest-first order)
        currentGroup.Insert(0, newsItem);

        // Save updated group
        await newsCacheRepo.SetNewsAsync(currentGroupKey, currentGroup, TimeSpan.FromDays(180)); // 6 months expiration
      }

      // Update the latest group number metadata
      await newsCacheRepo.SetLatestGroupNumberAsync(currentGroupNumber);
    }
    catch (Exception ex)
    {
      logger.LogError(ex, "Error updating news groups");
      throw;
    }
  }
}