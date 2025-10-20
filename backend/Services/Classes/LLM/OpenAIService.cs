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
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Client;
using backend.Repository.Interfaces;
using Org.BouncyCastle.Crypto.Engines;

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
  public async Task<(string response, int totalTokenUsed)> GenerateResponseAsync(
    string userMessage,
    List<ChatMessage> context,
    CancellationToken cancellationToken,
    bool isChatRelatedToNewsSource,
    bool isInitialMessage,
    Func<string, Task>? onChunkReceived = null,
    string? relatedNewsSourceContent = null)
  {
    logger.LogWarning($"[GenerateResponse] Received content length: {relatedNewsSourceContent?.Length ?? 0}");
    var responseBuilder = new StringBuilder();
    int totalTokensUsed = 0;

    try
    {
      // Build base messages
      var messages = BuildBaseMessages(userMessage, context, isChatRelatedToNewsSource, relatedNewsSourceContent);

      // Early return for news-related chats without tool calling
      if (isChatRelatedToNewsSource && isInitialMessage)
      {
        var result = await StreamChatResponseAsync(messages, responseBuilder, totalTokensUsed, onChunkReceived, cancellationToken);
        totalTokensUsed = result.tokensUsed;
        return result;
      }

      // Get available tools
      var availableTools = mcpClient.GetAllAvailableToolsAsync();
      logger.LogInformation($"Available MCP tools count: {availableTools.Count}");

      if (availableTools.Count == 0)
      {
        logger.LogInformation("No MCP tools available - proceeding without tools");
        var result = await StreamChatResponseAsync(messages, responseBuilder, totalTokensUsed, onChunkReceived, cancellationToken);
        totalTokensUsed = result.tokensUsed;
        return result;
      }

      // Create options with tools
      var chatOptions = new ChatCompletionOptions
      {
        MaxOutputTokenCount = settings.MaxToken
      };

      // Add tools to options
      foreach (var tool in availableTools)
      {
        var chatTool = ConvertMcpToolToChatTool(tool);
        if (chatTool != null)
        {
          chatOptions.Tools.Add(chatTool);
        }
      }

      // Start streaming with tools enabled
      bool requiresToolExecution = false;
      List<ChatToolCall> toolCalls = new();
      var toolCallsBuilder = new Dictionary<int, StringBuilder>();
      var toolCallsInfo = new Dictionary<int, (string id, string functionName)>();

      await foreach (var update in chatClient.CompleteChatStreamingAsync(messages, chatOptions, cancellationToken))
      {
        // Handle content updates (regular text response)
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

        // Handle tool call updates (streaming tool calls)
        if (update.ToolCallUpdates.Count > 0)
        {
          foreach (var toolCallUpdate in update.ToolCallUpdates)
          {
            int index = toolCallUpdate.Index;

            // Initialize builders for this tool call index
            if (!toolCallsBuilder.ContainsKey(index))
            {
              toolCallsBuilder[index] = new StringBuilder();
            }

            // Accumulate tool call ID
            if (!string.IsNullOrEmpty(toolCallUpdate.ToolCallId))
            {
              if (!toolCallsInfo.ContainsKey(index))
              {
                toolCallsInfo[index] = (toolCallUpdate.ToolCallId, string.Empty);
              }
            }

            // Accumulate function name
            if (!string.IsNullOrEmpty(toolCallUpdate.FunctionName))
            {
              var current = toolCallsInfo.GetValueOrDefault(index, (string.Empty, string.Empty));
              toolCallsInfo[index] = (current.Item1, toolCallUpdate.FunctionName);
            }

            // Accumulate function arguments (comes as BinaryData)
            if (toolCallUpdate.FunctionArgumentsUpdate != null && toolCallUpdate.FunctionArgumentsUpdate.ToMemory().Length > 0)
            {
              toolCallsBuilder[index].Append(toolCallUpdate.FunctionArgumentsUpdate.ToString());
            }

            requiresToolExecution = true;
          }
        }

        // Track token usage
        if (update.Usage != null)
        {
          totalTokensUsed += update.Usage.TotalTokenCount;
        }

        // Check finish reason
        if (update.FinishReason == ChatFinishReason.ToolCalls)
        {
          requiresToolExecution = true;
        }
      }

      // If tools were called, execute them and continue
      if (requiresToolExecution && toolCallsInfo.Count > 0)
      {
        logger.LogInformation($"LLM requested {toolCallsInfo.Count} tool calls during streaming");

        // Build ChatToolCall objects from accumulated data
        foreach (var kvp in toolCallsInfo)
        {
          int index = kvp.Key;
          var (id, functionName) = kvp.Value;
          var arguments = toolCallsBuilder[index].ToString();

          toolCalls.Add(ChatToolCall.CreateFunctionToolCall(id, functionName, BinaryData.FromString(arguments)));
        }

        // Add assistant message with tool calls to conversation
        messages.Add(new AssistantChatMessage(toolCalls));

        // Execute each tool call and add results
        foreach (var toolCall in toolCalls)
        {
          await ExecuteToolAndAddResultMessage(messages, toolCall);
        }

        // Make another streaming request with tool results
        responseBuilder.Clear(); // Clear any partial content before tool execution
        var finalResult = await StreamChatResponseAsync(messages, responseBuilder, totalTokensUsed, onChunkReceived, cancellationToken);
        totalTokensUsed = finalResult.tokensUsed;
        return finalResult;
      }

      return (responseBuilder.ToString(), totalTokensUsed);
    }
    catch (OperationCanceledException)
    {
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

  private async Task<(string response, int tokensUsed)> StreamChatResponseAsync(
      List<OpenAI.Chat.ChatMessage> messages,
      StringBuilder responseBuilder,
      int currentTokensUsed,
      Func<string, Task>? onChunkReceived,
      CancellationToken cancellationToken)
  {
    int totalTokensUsed = currentTokensUsed;
    var options = new ChatCompletionOptions
    {
      MaxOutputTokenCount = settings.MaxToken
    };

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
        totalTokensUsed += update.Usage.TotalTokenCount;
      }
    }

    return (responseBuilder.ToString(), totalTokensUsed);
  }

  private ChatTool? ConvertMcpToolToChatTool(McpClientTool mcpTool)
  {
    try
    {
      var schema = GetToolSchema(mcpTool);
      var schemaBinaryData = BinaryData.FromString(schema);

      return ChatTool.CreateFunctionTool(
          functionName: mcpTool.Name,
          functionDescription: mcpTool.Description ?? "No description available",
          functionParameters: schemaBinaryData
      );
    }
    catch (Exception ex)
    {
      logger.LogWarning(ex, $"Failed to convert MCP tool '{mcpTool.Name}' to ChatTool");
      return null;
    }
  }

  private async Task ExecuteToolAndAddResultMessage(List<OpenAI.Chat.ChatMessage> messages, ChatToolCall toolCall)
  {
    try
    {
      logger.LogInformation($"Executing tool: '{toolCall.FunctionName}'");

      var arguments = JsonSerializer.Deserialize<Dictionary<string, object?>>(toolCall.FunctionArguments.ToString())
                      ?? new Dictionary<string, object?>();

      var toolResult = await mcpClient.CallToolAsync(toolCall.FunctionName, arguments);
      var resultText = SerializeToolResult(toolResult, toolCall.FunctionName);

      messages.Add(new ToolChatMessage(toolCall.Id, resultText));

      logger.LogInformation($"Tool '{toolCall.FunctionName}' executed successfully");
    }
    catch (Exception ex)
    {
      logger.LogError(ex, $"Failed to execute tool '{toolCall.FunctionName}'");
      messages.Add(new ToolChatMessage(toolCall.Id, $"Error executing tool: {ex.Message}"));
    }
  }

  private string SerializeToolResult(CallToolResult result, string toolName)
  {
    var serialized = JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = false });

    logger.LogInformation($"Tool '{toolName}' executed successfully! Result length: {serialized.Length} chars");
    logger.LogDebug($"Tool result preview: {(serialized.Length > 200 ? serialized.Substring(0, 200) + "..." : serialized)}");

    return serialized;
  }

  private List<OpenAI.Chat.ChatMessage> BuildBaseMessages(string userMessage, List<ChatMessage> context, bool isChatRelatedToNewsSource, string? relatedNewsSourceContent = null)
  {
    var messages = new List<OpenAI.Chat.ChatMessage>();
    logger.LogInformation(relatedNewsSourceContent);
    if (isChatRelatedToNewsSource)
    {
      messages.Add(new SystemChatMessage(
            $"Reference Content:\n{relatedNewsSourceContent}\n\n" +
            $"Use this content to provide accurate answers based on the source material."
        ));
    }

    messages.Add(new SystemChatMessage(PromptConstants.SYSTEM_PROMPT));

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