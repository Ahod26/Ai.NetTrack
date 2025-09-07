using backend.Models.Domain;

namespace backend.Services.Interfaces.LLM;

public interface IOpenAIService
{
  Task<(string response, int totalTokenUsed)> GenerateResponseAsync(string userMessage, List<ChatMessage> context, CancellationToken cancellationToken, Func<string, Task>? onChunkReceived = null);
  Task<string> GenerateChatTitle(string firstMessage);
  Task<List<NewsItem>> ProcessGitHubData(string prompt);
}