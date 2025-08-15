public interface IOpenAIService
{
  Task<string> GenerateResponseAsync(string userMessage, List<ChatMessage> context, Func<string, Task>? onChunkReceived = null);
}