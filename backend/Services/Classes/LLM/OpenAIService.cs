using System.Text;
using backend.Models.Configuration;
using Microsoft.Extensions.Options;
using OpenAI.Chat;
using backend.Models.Domain;
using backend.Constants;
using ChatMessage = backend.Models.Domain.ChatMessage;
using backend.Services.Interfaces.LLM;
using System.Text.Json;
using backend.MCP.Interfaces;
using OpenAI.Audio;

namespace backend.Services.Classes.LLM;

public class OpenAIService(
  ChatClient chatClient,
  IOptions<OpenAISettings> options,
  ILogger<OpenAIService> logger,
  IMcpClientService mcpClient
) : IOpenAIService
{
  private readonly OpenAISettings settings = options.Value;
  public async Task<(string response, int totalTokenUsed)> GenerateResponseAsync(string userMessage, List<ChatMessage> context, CancellationToken cancellationToken, Func<string, Task>? onChunkReceived = null)
  {
    var responseBuilder = new StringBuilder();
    int totalTokensUsed = 0;
    try
    {
      // Build messages with optional tool result
      var (messages, toolTokens) = await BuildMessagesWithOptionalToolAsync(userMessage, context, cancellationToken);
      totalTokensUsed += toolTokens;

      // 2) ANSWER PHASE (streaming final assistant response)
      var answerOptions = new ChatCompletionOptions
      {
        MaxOutputTokenCount = settings.MaxToken
      };

      await foreach (var update in chatClient.CompleteChatStreamingAsync(messages, answerOptions, cancellationToken))
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
          totalTokensUsed += update.Usage.TotalTokenCount; // accumulate
        }
      }

      return (responseBuilder.ToString(), totalTokensUsed);
    }
    catch (OperationCanceledException)
    {
      // streaming was cancelled
      // Return the partial content accumulated so far so i can save it to DB
      return (responseBuilder.ToString(), totalTokensUsed);
    }
    catch (Exception ex)
    {
      logger.LogError(ex, "Error calling OpenAI API");
      return ("Sorry, I'm having trouble responding right now. Please try again.", 0);
    }
  }

  private async Task<(List<OpenAI.Chat.ChatMessage> messages, int tokensUsed)> BuildMessagesWithOptionalToolAsync(string userMessage, List<ChatMessage> context, CancellationToken cancellationToken)
  {
    int tokensUsed = 0;

    // Build base messages list using existing system prompt
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

    // 1) TOOL DECISION PHASE 
    var availableTools = mcpClient.GetAllAvailableToolsAsync();
    if (availableTools.Count > 0)
    {
      var toolsCatalog = string.Join("\n\n", availableTools.Select(t =>
      {
        var schema = "{}";
        try
        {
          if (t.ProtocolTool?.InputSchema != null)
          {
            schema = JsonSerializer.Serialize(t.ProtocolTool.InputSchema, new JsonSerializerOptions { WriteIndented = true });
          }
        }
        catch
        {
          schema = "{}";
        }

        return $"Tool: {t.Name}\nDescription: {t.Description ?? "No description available"}\nParameters Schema:\n{schema}";
      }));
      
      var decisionInstruction = PromptConstants.GetToolDecisionPrompt(toolsCatalog);      // Insert decision system message just before the user message (already added) or at end before decision call.

      var decisionMessages = new List<OpenAI.Chat.ChatMessage>();
      decisionMessages.AddRange(messages); // system + context + user
      decisionMessages.Add(new SystemChatMessage(decisionInstruction));

      var decisionOptions = new ChatCompletionOptions
      {
        MaxOutputTokenCount = 200
      };

      var decision = await chatClient.CompleteChatAsync(decisionMessages, decisionOptions, cancellationToken);
      var decisionText = decision.Value.Content[0].Text?.Trim();
      if (decision.Value.Usage != null)
      {
        tokensUsed += decision.Value.Usage.TotalTokenCount;
      }

      if (!string.IsNullOrWhiteSpace(decisionText) && !string.Equals(decisionText, "NO_TOOL", StringComparison.OrdinalIgnoreCase))
      {
        try
        {
          // Attempt to parse tool JSON
          var jsonDoc = JsonDocument.Parse(decisionText);
          if (jsonDoc.RootElement.TryGetProperty("tool", out var toolNameEl))
          {
            var toolName = toolNameEl.GetString() ?? string.Empty;
            if (!string.IsNullOrWhiteSpace(toolName))
            {
              Dictionary<string, object?> args = [];
              if (jsonDoc.RootElement.TryGetProperty("arguments", out var argsEl) && argsEl.ValueKind == JsonValueKind.Object)
              {
                try
                {
                  args = JsonSerializer.Deserialize<Dictionary<string, object?>>(argsEl.GetRawText()) ?? new();
                }
                catch { /* ignore parse issues */ }
              }

              try
              {
                var toolResult = await mcpClient.CallToolAsync(toolName, args);
                var serialized = JsonSerializer.Serialize(toolResult, new JsonSerializerOptions { WriteIndented = false });
                // Truncate overly long tool responses to protect token budget
                const int maxToolResultChars = 4000;
                if (serialized.Length > maxToolResultChars)
                {
                  serialized = serialized.Substring(0, maxToolResultChars) + "...<truncated>";
                }
                messages.Add(new SystemChatMessage($"Tool result from '{toolName}':\n{serialized}\nUse this information to craft the best answer."));
              }
              catch (Exception ex)
              {
                messages.Add(new SystemChatMessage($"Tool '{toolName}' invocation failed: {ex.Message}. Proceed without tool."));
              }
            }
          }
        }
        catch
        {
          // If JSON parse fails, just continue without tool
        }
      }
      else
      {
        // Add explicit note so model knows no tool was executed if we want; optional (can omit)
        messages.Add(new SystemChatMessage("No external tool used."));
      }
    }

    return (messages, tokensUsed);
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