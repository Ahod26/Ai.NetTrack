public interface ILLMCacheRepo
{
  Task StoreExactCacheAsync(string cacheKey, string response, TimeSpan expiration);
  Task<string?> GetExactCachedResponseAsync(string cacheKey);
  Task ClearAllCacheAsync();
  Task InitializeRedisIndexAsync();
  Task<(string DocumentId, float Score)?> SearchSemanticCacheAsync(byte[] queryVector, int messageCount);
  Task<string?> GetResponseFromSemanticMatchAsync(string documentId);
  Task StoreSemanticCacheAsync(string response, byte[] embeddingBytes, int messageCount, List<string> topics, TimeSpan expiration, string fullContext);
  Task RecreateIndexAsync();
  Task RefreshIndexAsync();
}