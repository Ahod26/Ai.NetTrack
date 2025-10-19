namespace backend.Repository.Interfaces;

public interface ILLMCacheRepo
{
  Task StoreExactCacheAsync(string cacheKey, string response, TimeSpan expiration, bool isURL);
  Task<string?> GetExactCachedResponseAsync(string cacheKey, bool isURL);
  Task ClearAllCacheAsync();
  Task InitializeRedisIndexAsync();
  Task<(string DocumentId, float Score)?> SearchSemanticCacheAsync(byte[] queryVector, int messageCount);
  Task<string?> GetResponseFromSemanticMatchAsync(string documentId);
  Task StoreSemanticCacheAsync(string response, byte[] embeddingBytes, int messageCount, List<string> topics, TimeSpan expiration, string fullContext);
  Task RecreateIndexAsync();
  Task RefreshIndexAsync();
}