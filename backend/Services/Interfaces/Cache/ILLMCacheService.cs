using backend.Models.Domain;

namespace backend.Services.Interfaces.Cache;

public interface ILLMCacheService
{
  Task<string?> GetCachedResponseAsync(string userMessage, List<ChatMessage> context);
  Task SetCachedResponseAsync(string userMessage, List<ChatMessage> context, string response);
  Task<string?> GetCachedResponseForNewsResourceAsync(string url);
  Task SetCachedResponseForNewsResourceAsync(string url, string response);
}