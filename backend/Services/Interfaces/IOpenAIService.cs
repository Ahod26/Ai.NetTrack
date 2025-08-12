public interface IOpenAIService
{
  Task<string> GenerateResponseAsync(string userMessage, List<ChatMessage> context);
}