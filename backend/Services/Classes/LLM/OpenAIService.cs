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
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Client;

namespace backend.Services.Classes.LLM;

public class OpenAIService(
  ChatClient chatClient,
  IOptions<OpenAISettings> options,
  ILogger<OpenAIService> logger,
  IMcpClientService mcpClient
) : IOpenAIService
{
  private readonly OpenAISettings settings = options.Value;

  #region Public Interface Methods
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
      ResponseFormat = ChatResponseFormat.CreateJsonObjectFormat() // Enforce JSON format
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

  #endregion


  #region Private Implementation Methods
  private async Task<(List<OpenAI.Chat.ChatMessage> messages, int tokensUsed)> BuildMessagesWithOptionalToolAsync(
      string userMessage,
      List<ChatMessage> context,
      CancellationToken cancellationToken)
  {
    int tokensUsed = 0;

    var messages = BuildBaseMessages(userMessage, context);

    var availableTools = mcpClient.GetAllAvailableToolsAsync();
    logger.LogInformation($"Available MCP tools count: {availableTools.Count}");

    if (availableTools.Count == 0)
    {
      logger.LogInformation("No MCP tools available - skipping tool decision phase");
      return (messages, tokensUsed);
    }

    var toolsCatalog = BuildToolsCatalog(availableTools);
    var decisionText = await GetToolDecisionFromLLM(messages, toolsCatalog, cancellationToken);
    tokensUsed += decisionText.tokensUsed;

    if (ShouldUseTool(decisionText.response))
    {
      await ExecuteToolAndAddResult(messages, decisionText.response);
    }
    else
    {
      logger.LogInformation("LLM decided no tool is needed");
    }

    return (messages, tokensUsed);
  }

  private async Task<(string response, int tokensUsed)> GetToolDecisionFromLLM(
      List<OpenAI.Chat.ChatMessage> messages,
      string toolsCatalog,
      CancellationToken cancellationToken)
  {
    var decisionInstruction = PromptConstants.GetToolDecisionPrompt(toolsCatalog);

    var decisionMessages = new List<OpenAI.Chat.ChatMessage>();
    decisionMessages.AddRange(messages);
    decisionMessages.Add(new SystemChatMessage(decisionInstruction));

    var decisionOptions = new ChatCompletionOptions
    {
      MaxOutputTokenCount = 200
    };

    logger.LogInformation("Asking LLM to decide if a tool is needed...");
    var decision = await chatClient.CompleteChatAsync(decisionMessages, decisionOptions, cancellationToken);

    var decisionText = decision.Value.Content[0].Text?.Trim() ?? string.Empty;
    var tokensUsed = decision.Value.Usage?.TotalTokenCount ?? 0;

    logger.LogInformation($"LLM Decision Response: '{decisionText}'");
    logger.LogDebug($"Decision phase used {tokensUsed} tokens");

    return (decisionText, tokensUsed);
  }

  private bool ShouldUseTool(string decisionText)
  {
    return !string.IsNullOrWhiteSpace(decisionText) &&
           !string.Equals(decisionText, "NO_TOOL", StringComparison.OrdinalIgnoreCase);
  }

  private async Task ExecuteToolAndAddResult(List<OpenAI.Chat.ChatMessage> messages, string decisionText)
  {
    logger.LogInformation("LLM wants to use a tool! Parsing decision...");

    var (toolName, arguments) = ParseToolDecision(decisionText);

    if (string.IsNullOrWhiteSpace(toolName))
    {
      logger.LogWarning("Failed to parse tool decision - no tool name found");
      return;
    }

    logger.LogInformation($"Calling MCP tool: '{toolName}'");
    logger.LogInformation($"Tool arguments: {JsonSerializer.Serialize(arguments)}");

    try
    {
      var toolResult = await mcpClient.CallToolAsync(toolName, arguments);
      var resultText = SerializeAndLimitToolResult(toolResult, toolName);

      messages.Add(new SystemChatMessage(
          $"Tool result from '{toolName}':\n{resultText}\n" +
          $"Use this information to craft the best answer."
      ));

      logger.LogInformation("Tool result added to conversation context");
    }
    catch (Exception ex)
    {
      logger.LogError(ex, $"Failed to execute tool '{toolName}'");
      messages.Add(new SystemChatMessage(
          $"Tool '{toolName}' invocation failed: {ex.Message}. Proceed without tool."
      ));
    }
  }

  private (string toolName, Dictionary<string, object?> arguments) ParseToolDecision(string decisionText)
  {
    try
    {
      var jsonDoc = JsonDocument.Parse(decisionText);

      if (!jsonDoc.RootElement.TryGetProperty("tool", out var toolNameEl))
      {
        logger.LogWarning("LLM decision JSON doesn't contain 'tool' property");
        return (string.Empty, new Dictionary<string, object?>());
      }

      var toolName = toolNameEl.GetString() ?? string.Empty;
      var args = new Dictionary<string, object?>();

      if (jsonDoc.RootElement.TryGetProperty("arguments", out var argsEl) &&
          argsEl.ValueKind == JsonValueKind.Object)
      {
        try
        {
          args = JsonSerializer.Deserialize<Dictionary<string, object?>>(argsEl.GetRawText())
                 ?? new Dictionary<string, object?>();
        }
        catch (Exception ex)
        {
          logger.LogWarning(ex, "Failed to parse tool arguments");
        }
      }

      return (toolName, args);
    }
    catch (Exception ex)
    {
      logger.LogError(ex, "Failed to parse LLM decision JSON");
      return (string.Empty, new Dictionary<string, object?>());
    }
  }

  private string SerializeAndLimitToolResult(CallToolResult result, string toolName)
  {
    var serialized = JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = false });

    logger.LogInformation($"Tool '{toolName}' executed successfully! Result length: {serialized.Length} chars");
    logger.LogDebug($"Tool result preview: {(serialized.Length > 200 ? serialized.Substring(0, 200) + "..." : serialized)}");

    const int maxToolResultChars = 4000;
    if (serialized.Length > maxToolResultChars)
    {
      serialized = serialized.Substring(0, maxToolResultChars) + "...<shortened>";
      logger.LogWarning($"Tool result shortened from {serialized.Length} to {maxToolResultChars} chars");
    }

    return serialized;
  }

  private List<OpenAI.Chat.ChatMessage> BuildBaseMessages(string userMessage, List<ChatMessage> context)
  {
    var messages = new List<OpenAI.Chat.ChatMessage>
    {
        new SystemChatMessage(PromptConstants.SYSTEM_PROMPT)
    };

    foreach (var msg in context)
    {
      if (msg.Type == MessageType.User)
        messages.Add(new UserChatMessage(msg.Content));
      else
        messages.Add(new AssistantChatMessage(msg.Content));
    }

    messages.Add(new UserChatMessage(userMessage));
    return messages;
  }

  private string BuildToolsCatalog(IList<McpClientTool> tools)
  {
    var catalog = string.Join("\n\n", tools.Select(t =>
    {
      var schema = GetToolSchema(t);
      return $"Tool: {t.Name}\n" +
             $"Description: {t.Description ?? "No description available"}\n" +
             $"Parameters Schema:\n{schema}";
    }));

    logger.LogInformation($"Sending tool catalog to LLM for decision (catalog length: {catalog.Length} chars)");
    return catalog;
  }

  private string GetToolSchema(McpClientTool tool)
  {
    try
    {
      if (tool.ProtocolTool?.InputSchema != null)
      {
        return JsonSerializer.Serialize(
            tool.ProtocolTool.InputSchema,
            new JsonSerializerOptions { WriteIndented = true }
        );
      }
    }
    catch
    {
      // Silently fall through to default
    }

    return "{}";
  }

  #endregion
}