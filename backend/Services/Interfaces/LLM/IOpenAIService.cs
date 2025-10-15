using backend.Models.Domain;

namespace backend.Services.Interfaces.LLM;

public interface IOpenAIService
{
  Task<(string response, int totalTokenUsed)> GenerateResponseAsync(string userMessage, List<ChatMessage> context, CancellationToken cancellationToken, bool isChatRelatedToNewsSource, bool isInitialMessage, Func<string, Task>? onChunkReceived = null, string? relatedNewsSourceContent = null);
  Task<string> GenerateChatTitle(string firstMessage);
  Task<List<NewsItem>> ProcessNewsData(string prompt);
}