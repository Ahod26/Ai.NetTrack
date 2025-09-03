using backend.Models.Domain;

namespace backend.Services.Interfaces;

public interface ILLMCacheService
{
  Task<string?> GetCachedResponseAsync(string userMessage, List<ChatMessage> context);
  Task SetCachedResponseAsync(string userMessage, List<ChatMessage> context, string response);
}