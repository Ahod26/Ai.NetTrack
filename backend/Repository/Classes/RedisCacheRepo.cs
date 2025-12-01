using System.Text.Json;
using backend.Models.Configuration;
using backend.Models.Domain;
using backend.Models.Dtos;
using backend.Repository.Interfaces;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace backend.Repository.Classes;

public class RedisCacheRepo(
  IConnectionMultiplexer redis,
  ILogger<RedisCacheRepo> logger,
  IOptions<NewsCacheSettings> options) : IRedisCacheRepo
{
  private readonly IDatabase _database = redis.GetDatabase();
  private readonly NewsCacheSettings settings = options.Value;

  #region News Related Functions
  public async Task<List<NewsItem>?> GetNewsAsync(string dateKey, int newsType)
  {
    try
    {
      var cachedData = await _database.StringGetAsync(dateKey);

      if (cachedData.HasValue && newsType != 0)
      {
        var dataList = JsonSerializer.Deserialize<List<NewsItem>>(cachedData!);
        return dataList!.Where(n => (int)n.SourceType == newsType).ToList();
      }

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
      await _database.StringSetAsync(dateKey, serializedNews, settings.CacheDuration);
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

  #endregion

  
  #region Newsletter Related Functions
  public async Task AddUserToNewsletterListAsync(string cacheKey, EmailNewsletterDTO user)
  {
    try
    {
      var serializedUser = JsonSerializer.Serialize(user);
      await _database.ListRightPushAsync(cacheKey, serializedUser);

      logger.LogInformation("Added user {Email} to newsletter list", user.Email);
    }
    catch (Exception ex)
    {
      logger.LogError(ex, "Error adding user to newsletter list");
      throw;
    }
  }

  public async Task RemoveUserFromNewsletterListAsync(string cacheKey, string email)
  {
    try
    {
      var allUsers = await _database.ListRangeAsync(cacheKey);

      foreach (var userJson in allUsers)
      {
        var user = JsonSerializer.Deserialize<EmailNewsletterDTO>(userJson!);
        if (user?.Email == email)
        {
          await _database.ListRemoveAsync(cacheKey, userJson);
          logger.LogInformation("Removed user {Email} from newsletter list", email);
          return;
        }
      }

      logger.LogWarning("User {Email} not found in newsletter list", email);
    }
    catch (Exception ex)
    {
      logger.LogError(ex, "Error removing user from newsletter list");
      throw;
    }
  }

  public async Task<List<EmailNewsletterDTO>> GetNewsletterSubscribersListAsync(string cacheKey)
  {
    try
    {
      var allUsers = await _database.ListRangeAsync(cacheKey);
      var subscribers = new List<EmailNewsletterDTO>();

      foreach (var userJson in allUsers)
      {
        var user = JsonSerializer.Deserialize<EmailNewsletterDTO>(userJson!);
        if (user != null)
        {
          subscribers.Add(user);
        }
      }

      return subscribers;
    }
    catch (Exception ex)
    {
      logger.LogError(ex, "Error getting newsletter subscribers list");
      return [];
    }
  }

  public async Task<bool> IsUserInNewsletterListAsync(string cacheKey, string email)
  {
    try
    {
      var allUsers = await _database.ListRangeAsync(cacheKey);

      foreach (var userJson in allUsers)
      {
        var user = JsonSerializer.Deserialize<EmailNewsletterDTO>(userJson!);
        if (user?.Email == email)
        {
          return true;
        }
      }

      return false;
    }
    catch (Exception ex)
    {
      logger.LogError(ex, "Error checking if user is subscribed");
      return false;
    }
  }
  
  #endregion
}