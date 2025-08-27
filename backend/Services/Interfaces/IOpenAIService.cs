public interface IOpenAIService
{
  Task<string> GenerateResponseAsync(string userMessage, List<ChatMessage> context, CancellationToken cancellationToken, Func<string, Task>? onChunkReceived = null);
  Task<string> GenerateChatTitle(string firstMessage);
}