using System.Text.Json;
using backend.Models.Domain;
using backend.Repository.Interfaces;
using StackExchange.Redis;

namespace backend.Repository.Classes;

public class NewsCacheRepo(IConnectionMultiplexer redis, ILogger<NewsCacheRepo> logger) : INewsCacheRepo
{
  private readonly IDatabase _database = redis.GetDatabase();

  public async Task<List<NewsItem>?> GetNewsByDateAsync(string dateKey)
  {
    try
    {
      var cachedData = await _database.StringGetAsync(dateKey);
      if (cachedData.HasValue)
      {
        return JsonSerializer.Deserialize<List<NewsItem>>(cachedData!);
      }
      return null;
    }
    catch (Exception ex)
    {
      logger.LogError(ex, "Error getting cached news for date key {DateKey}", dateKey);
      return null;
    }
  }

  public async Task SetNewsByDateAsync(string dateKey, List<NewsItem> news)
  {
    try
    {
      var serializedNews = JsonSerializer.Serialize(news);
      // Set expiration to 15 days, news doesn't change
      await _database.StringSetAsync(dateKey, serializedNews, TimeSpan.FromDays(15));
    }
    catch (Exception ex)
    {
      logger.LogError(ex, "Error setting cached news for date key {DateKey}", dateKey);
    }
  }

  public async Task ClearAllNewsCacheAsync()
  {
    try
    {
      var server = redis.GetServer(redis.GetEndPoints().First());
      var keys = server.Keys(pattern: "news:date:*");

      if (keys.Any())
      {
        await _database.KeyDeleteAsync(keys.ToArray());
        logger.LogInformation("Cleared {Count} news cache keys", keys.Count());
      }
    }
    catch (Exception ex)
    {
      logger.LogError(ex, "Error clearing all news cache");
      throw;
    }
  }

}