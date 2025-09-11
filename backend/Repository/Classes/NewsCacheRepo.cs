using System.Text.Json;
using backend.Models.Domain;
using backend.Repository.Interfaces;
using OpenAI.FineTuning;
using StackExchange.Redis;

namespace backend.Repository.Classes;

public class NewsCacheRepo(IConnectionMultiplexer redis, ILogger<NewsCacheRepo> logger) : INewsCacheRepo
{
  private readonly IDatabase _database = redis.GetDatabase();

  public async Task<List<NewsItem>?> GetNewsAsync(string cachedKey)
  {
    try
    {
      var cachedData = await _database.StringGetAsync(cachedKey);
      if (cachedData.HasValue)
      {
        return JsonSerializer.Deserialize<List<NewsItem>>(cachedData!);
      }
      return null;
    }
    catch (Exception ex)
    {
      logger.LogError(ex, "Error getting cached news");
      return null;
    }
  }

  public async Task SetNewsAsync(string cacheKey, List<NewsItem> news, TimeSpan expiration)
  {
    try
    {
      var serializedNews = JsonSerializer.Serialize(news);
      await _database.StringSetAsync(cacheKey, serializedNews, expiration);
    }
    catch (Exception ex)
    {
      logger.LogError(ex, "Error setting cached news for key {CacheKey}", cacheKey);
    }
  }

  public async Task DeleteNewsAsync(string cacheKey)
  {
    try
    {
      await _database.KeyDeleteAsync(cacheKey);
    }
    catch (Exception ex)
    {
      logger.LogError(ex, "Error deleting cached news for key {CacheKey}", cacheKey);
    }
  }

  public async Task<int> GetLatestGroupNumberAsync()
  {
    try
    {
      var groupNumberData = await _database.StringGetAsync("news:latest_group");
      if (groupNumberData.HasValue)
      {
        return int.Parse(groupNumberData!);
      }
      return 1; // Start with group 1 if no groups exist
    }
    catch (Exception ex)
    {
      logger.LogWarning(ex, "Error getting latest group number, defaulting to 1");
      return 1;
    }
  }

  public async Task SetLatestGroupNumberAsync(int groupNumber)
  {
    try
    {
      await _database.StringSetAsync("news:latest_group", groupNumber.ToString());
    }
    catch (Exception ex)
    {
      logger.LogError(ex, "Error setting latest group number");
    }
  }
}