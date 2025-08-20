public interface ILLMCacheService
{
  Task<string?> GetCachedResponseAsync(string cacheKey);
  Task SetCachedResponseAsync(string cacheKey, string response, TimeSpan? expiration = null);
  Task RemoveCachedResponseAsync(string cacheKey);

  string GenerateCacheKey(string prompt, List<ChatMessage> context);

  Task RemoveByPatternAsync(string pattern);
  Task ClearAllCacheAsync();

  // Task<bool> ExistsAsync(string cacheKey);
  //optional, maybe in the future
}