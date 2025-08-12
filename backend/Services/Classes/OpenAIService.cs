using OpenAI.Chat;

public class OpenAIService(
  ChatClient chatClient, IConfiguration configuration, ILogger<OpenAIService> logger
) : IOpenAIService
{
  public async Task<string> GenerateResponseAsync(string userMessage, List<ChatMessage> context)
  {
    try
    {
      // Build messages list using new message types
      var messages = new List<OpenAI.Chat.ChatMessage>
      {
          // Generic system message
        new SystemChatMessage("You are a helpful AI assistant.")
      };

      // Add context (previous messages)
      foreach (var msg in context.TakeLast(10))
      {
        if (msg.Type == MessageType.User)
        {
          messages.Add(new UserChatMessage(msg.Content));
        }
        else
        {
          messages.Add(new AssistantChatMessage(msg.Content));
        }
      }

      // Add current user message
      messages.Add(new UserChatMessage(userMessage));

      // Create completion options
      var options = new ChatCompletionOptions
      {
        MaxOutputTokenCount = int.Parse(configuration["OpenAI:MaxTokens"] ?? "4096"),
        Temperature = float.Parse(configuration["OpenAI:Temperature"] ?? "0.7")
      };

      // Call OpenAI with new API
      var completion = await chatClient.CompleteChatAsync(messages, options);

      return completion.Value.Content[0].Text;
    }
    catch (Exception ex)
    {
      logger.LogError(ex, "Error calling OpenAI API");
      return "Sorry, I'm having trouble responding right now. Please try again.";
    }
  }
}
