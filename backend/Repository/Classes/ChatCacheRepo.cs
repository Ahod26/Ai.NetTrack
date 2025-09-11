using System.Text.Json;
using StackExchange.Redis;
using backend.Models.Dtos;
using backend.Models.Domain;
using backend.Repository.Interfaces;

namespace backend.Repository.Classes;

public class ChatCacheRepo(IConnectionMultiplexer redis, ILogger<ChatCacheRepo> logger) : IChatCacheRepo
{
  private readonly IDatabase _database = redis.GetDatabase();

  public async Task<CachedChatData?> GetCachedChatAsync(string cacheKey)
  {
    try
    {
      var cachedData = await _database.StringGetAsync(cacheKey);

      if (cachedData.HasValue)
      {
        return JsonSerializer.Deserialize<CachedChatData>(cachedData!);
      }
      return null;
    }
    catch (Exception ex)
    {
      logger.LogError(ex, "Error getting cached chat for key {CacheKey}", cacheKey);
      return null;
    }
  }

  public async Task SetCachedChatAsync(string cacheKey, CachedChatData data, TimeSpan expiration)
  {
    try
    {
      var serializedData = JsonSerializer.Serialize(data);
      await _database.StringSetAsync(cacheKey, serializedData, expiration);
    }
    catch (Exception ex)
    {
      logger.LogError(ex, "Error setting cached chat for key {CacheKey}", cacheKey);
    }
  }

  public async Task UpdateCachedChatAsync(string cacheKey, CachedChatData data, TimeSpan expiration)
  {
    try
    {
      var exists = await _database.KeyExistsAsync(cacheKey);
      if (exists)
      {
        var serializedData = JsonSerializer.Serialize(data);
        await _database.StringSetAsync(cacheKey, serializedData, expiration);
      }
    }
    catch (Exception ex)
    {
      logger.LogError(ex, "Error updating cached chat for key {CacheKey}", cacheKey);
    }
  }

  public async Task DeleteCachedChatAsync(string cacheKey)
  {
    try
    {
      await _database.KeyDeleteAsync(cacheKey);
    }
    catch (Exception ex)
    {
      logger.LogError(ex, "Error deleting cached chat for key {CacheKey}", cacheKey);
    }
  }

  public async Task<List<ChatMessage>> GetAllStarredMessagesAsync(string userKeyPattern)
  {
    try
    {
      var server = _database.Multiplexer.GetServer(_database.Multiplexer.GetEndPoints()[0]);
      // Returns all chat keys for this user
      var chatKeys = server.Keys(pattern: userKeyPattern);

      var starredMessages = new List<ChatMessage>();

      foreach (var chatKey in chatKeys)
      {
        var cachedData = await _database.StringGetAsync(chatKey);
        if (cachedData.HasValue)
        {
          var chatData = JsonSerializer.Deserialize<CachedChatData>(cachedData!);
          if (chatData?.Messages != null)
          {
            var starredInChat = chatData.Messages.Where(m => m.IsStarred).ToList();
            starredMessages.AddRange(starredInChat);
          }
        }
      }

      return starredMessages;
    }
    catch (Exception ex)
    {
      logger.LogError(ex, "Error getting starred messages for pattern {Pattern}", userKeyPattern);
      return new List<ChatMessage>();
    }
  }

  public async Task UpdateMessageStarStatusAsync(string userKeyPattern, Guid messageId, bool isStarred)
  {
    try
    {
      var server = _database.Multiplexer.GetServer(_database.Multiplexer.GetEndPoints()[0]);
      // Returns all chat keys for this user
      var chatKeys = server.Keys(pattern: userKeyPattern);

      foreach (var chatKey in chatKeys)
      {
        var cachedData = await _database.StringGetAsync(chatKey);
        if (cachedData.HasValue)
        {
          var chatData = JsonSerializer.Deserialize<CachedChatData>(cachedData!);
          if (chatData?.Messages != null)
          {
            var messageToUpdate = chatData.Messages.FirstOrDefault(m => m.Id == messageId);
            if (messageToUpdate != null)
            {
              messageToUpdate.IsStarred = isStarred;

              var serializedData = JsonSerializer.Serialize(chatData);
              // Keep the original expiration by getting the TTL
              var ttl = await _database.KeyTimeToLiveAsync(chatKey);
              var expiration = ttl ?? TimeSpan.FromHours(2); // Default if no TTL

              await _database.StringSetAsync(chatKey, serializedData, expiration);
              break;
            }
          }
        }
      }
    }
    catch (Exception ex)
    {
      logger.LogError(ex, "Error updating message star status for messageId {MessageId}", messageId);
    }
  }
}