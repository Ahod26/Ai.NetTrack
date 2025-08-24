public interface ILLMCacheService
{
  Task<string?> GetCachedResponseAsync(string userMessage, List<ChatMessage> context);
  Task SetCachedResponseAsync(string userMessage, List<ChatMessage> context, string response);
  
  // Task<bool> ExistsAsync(string cacheKey);
  //optional, maybe in the future
}