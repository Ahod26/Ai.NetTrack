public interface ILLMCacheService
{
  Task<string?> GetCachedResponseAsync(string userMessage, List<ChatMessage> context);
  Task SetCachedResponseAsync(string userMessage, List<ChatMessage> context, string response);
  Task RemoveCachedResponseAsync(string cacheKey);

  string GenerateCacheKey(string prompt, List<ChatMessage> context);

  Task RemoveByPatternAsync(string pattern);
  Task ClearAllCacheAsync();
  Task ForceReindexAsync();
  Task RefreshIndexAsync();

  // Task<bool> ExistsAsync(string cacheKey);
  //optional, maybe in the future
}