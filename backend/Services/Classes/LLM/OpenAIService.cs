using System.Text;
using backend.Models.Configuration;
using Microsoft.Extensions.Options;
using OpenAI.Chat;
using backend.Models.Domain;
using backend.Constants;
using ChatMessage = backend.Models.Domain.ChatMessage;
using backend.Services.Interfaces.LLM;
using System.Text.Json;

namespace backend.Services.Classes.LLM;

public class OpenAIService(
  ChatClient chatClient, IOptions<OpenAISettings> options, ILogger<OpenAIService> logger
) : IOpenAIService
{
  private readonly OpenAISettings settings = options.Value;
  public async Task<(string response, int totalTokenUsed)> GenerateResponseAsync(string userMessage, List<ChatMessage> context, CancellationToken cancellationToken, Func<string, Task>? onChunkReceived = null)
  {
    try
    {
      // Build messages list using new message types
      var messages = new List<OpenAI.Chat.ChatMessage>
       {
           new SystemChatMessage(PromptConstants.SYSTEM_PROMPT)
       };

      // Add context (previous messages)
      foreach (var msg in context)
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
        MaxOutputTokenCount = settings.MaxToken
      };

      var responseBuilder = new StringBuilder();
      int totalTokensUsed = 0;

      // Use streaming API
      await foreach (var update in chatClient.CompleteChatStreamingAsync(messages, options, cancellationToken))
      {
        if (update.ContentUpdate.Count > 0)
        {
          var chunk = update.ContentUpdate[0].Text;
          if (!string.IsNullOrEmpty(chunk))
          {
            responseBuilder.Append(chunk);
            if (onChunkReceived != null)
            {
              await onChunkReceived(chunk);
            }
          }
        }
        if (update.Usage != null)
        {
          totalTokensUsed = update.Usage.TotalTokenCount;
        }
      }
      return (responseBuilder.ToString(), totalTokensUsed);
    }
    catch (OperationCanceledException)
    {
      logger.LogInformation("OpenAI streaming was cancelled");
      throw;
    }
    catch (Exception ex)
    {
      logger.LogError(ex, "Error calling OpenAI API");
      return ("Sorry, I'm having trouble responding right now. Please try again.", 0);
    }
  }

  public async Task<string> GenerateChatTitle(string firstMessage)
  {
    try
    {
      var messages = new List<OpenAI.Chat.ChatMessage>
    {
      new SystemChatMessage(PromptConstants.GET_TITLE_SYSTEM_PROMPT),

      new UserChatMessage($"Generate a title for this conversation: {firstMessage}")
    };

      var options = new ChatCompletionOptions
      {
        MaxOutputTokenCount = 50,
        Temperature = settings.TitleGenerationTemperature // Configurable temperature for consistent, focused titles
      };

      var completion = await chatClient.CompleteChatAsync(messages, options);

      var title = completion.Value.Content[0].Text?.Trim();

      logger.LogWarning(title);
      // Fallback if AI returns empty or very long title
      if (string.IsNullOrWhiteSpace(title) || title.Length > 25)
      {
        return "New chat";
      }

      return title;
    }
    catch (Exception ex)
    {
      logger.LogError(ex, "Error generating chat title with OpenAI API");
      return "New chat";
    }
  }

  public async Task<List<NewsItem>> ProcessNewsData(string prompt)
  {
    var chatOptions = new ChatCompletionOptions
    {
      ResponseFormat = ChatResponseFormat.CreateJsonObjectFormat()
    };

    var response = await chatClient.CompleteChatAsync(
      [
        new UserChatMessage(prompt)
      ],
      chatOptions
    );

    var jsonResponse = response.Value.Content[0].Text;
    logger.LogInformation($"LLM Response: {jsonResponse}");


    // The LLM return value is inconsistent, could return it wrapped with result object, and could return it without wrapper
    try
    {
      // First try to parse as direct array
      var newsItems = JsonSerializer.Deserialize<List<NewsItem>>(jsonResponse);
      return newsItems ?? new List<NewsItem>();
    }
    catch (JsonException)
    {
      try
      {
        // If that fails, try parsing as wrapped object
        using var doc = JsonDocument.Parse(jsonResponse);
        if (doc.RootElement.TryGetProperty("result", out var resultArray))
        {
          var newsItems = JsonSerializer.Deserialize<List<NewsItem>>(resultArray.GetRawText());
          return newsItems ?? new List<NewsItem>();
        }

        logger.LogWarning("LLM response doesn't contain 'result' property");
        return new List<NewsItem>();
      }
      catch (JsonException ex)
      {
        logger.LogError(ex, $"Failed to parse LLM JSON response: {jsonResponse}");
        return new List<NewsItem>();
      }
    }
  }
}
