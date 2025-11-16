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

    try
    {
      var messages = BuildBaseMessages(userMessage, context, isChatRelatedToNewsSource, relatedNewsSourceContent);
      bool isToolCallNeeded = ToolTriggers.ShouldUseMcpTools(userMessage);

      // Early return for news-related chats without tool calling OR messages that didn't pass keywords check
      if ((isChatRelatedToNewsSource && isInitialMessage) || !isToolCallNeeded)
      {
        if (!isToolCallNeeded)
        {
          logger.LogInformation("No AI-related keywords detected - skipping MCP tools");
        }
        return await StreamSimpleResponseAsync(messages, onChunkReceived, cancellationToken);
      }

      // Get available tools
      var availableTools = mcpClient.GetAllAvailableToolsAsync();
      logger.LogInformation($"Available MCP tools count: {availableTools.Count}");

      if (availableTools.Count == 0)
      {
        logger.LogInformation("No MCP tools available - proceeding without tools");
        return await StreamSimpleResponseAsync(messages, onChunkReceived, cancellationToken);
      }

      // Handle response with tools enabled
      return await StreamResponseWithToolsAsync(messages, availableTools, onChunkReceived, cancellationToken);
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

  private async Task<(string response, int totalTokenUsed)> StreamSimpleResponseAsync(
    List<OpenAI.Chat.ChatMessage> messages,
    Func<string, Task>? onChunkReceived,
    CancellationToken cancellationToken)
  {
    var responseBuilder = new StringBuilder();
    int totalTokensUsed = 0;

    var options = new ChatCompletionOptions
    {
      MaxOutputTokenCount = settings.MaxToken
    };
    try
    {
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
    }
    catch (OperationCanceledException)
    {
      logger.LogWarning($"Streaming cancelled - returning partial content ({responseBuilder.Length} chars)");
    }
    return (responseBuilder.ToString(), totalTokensUsed);
  }

  private async Task<(string response, int totalTokenUsed)> StreamResponseWithToolsAsync(
    List<OpenAI.Chat.ChatMessage> messages,
    IList<McpClientTool> availableTools,
    Func<string, Task>? onChunkReceived,
    CancellationToken cancellationToken)
  {
    var chatOptions = CreateChatOptionsWithTools(availableTools);
    var streamState = new StreamingState();

    try
    {
      // Stream first response with tool detection
      await foreach (var update in chatClient.CompleteChatStreamingAsync(messages, chatOptions, cancellationToken))
      {
        await ProcessStreamUpdate(update, streamState, onChunkReceived);
      }

      // If tools were called, execute them and get final response
      if (streamState.RequiresToolExecution)
      {
        return await ExecuteToolsAndGetFinalResponseAsync(
            messages,
            streamState,
            chatOptions,
            onChunkReceived,
            cancellationToken
        );
      }
    }
    catch (OperationCanceledException)
    {
      logger.LogWarning($"Initial streaming cancelled - returning partial content ({streamState.ResponseBuilder.Length} chars)");
    }


    return (streamState.ResponseBuilder.ToString(), streamState.TotalTokensUsed);
  }

  private async Task ProcessStreamUpdate(
    StreamingChatCompletionUpdate update,
    StreamingState state,
    Func<string, Task>? onChunkReceived)
  {
    // Handle regular content updates
    await HandleContentUpdates(update, state, onChunkReceived);

    // Handle tool call updates
    HandleToolCallUpdates(update, state);

    // Track token usage
    if (update.Usage != null)
    {
      state.TotalTokensUsed += update.Usage.TotalTokenCount;
    }

    // Check finish reason
    if (update.FinishReason == ChatFinishReason.ToolCalls)
    {
      state.RequiresToolExecution = true;
    }
  }

  private async Task HandleContentUpdates(
    StreamingChatCompletionUpdate update,
    StreamingState state,
    Func<string, Task>? onChunkReceived)
  {
    if (update.ContentUpdate.Count > 0)
    {
      var chunk = update.ContentUpdate[0].Text;
      if (!string.IsNullOrEmpty(chunk))
      {
        state.ResponseBuilder.Append(chunk);
        if (onChunkReceived != null)
        {
          await onChunkReceived(chunk);
        }
      }
    }
  }

  private void HandleToolCallUpdates(
    StreamingChatCompletionUpdate update,
    StreamingState state)
  {
    if (update.ToolCallUpdates.Count == 0) return;

    foreach (var toolCallUpdate in update.ToolCallUpdates)
    {
      int index = toolCallUpdate.Index;

      // Initialize builders for this tool call index if needed
      InitializeToolCallTracking(state, index);

      // Accumulate tool call data
      AccumulateToolCallData(state, index, toolCallUpdate);

      state.RequiresToolExecution = true;
    }
  }

  private void InitializeToolCallTracking(StreamingState state, int index)
  {
    if (!state.ToolCallsBuilder.ContainsKey(index))
    {
      state.ToolCallsBuilder[index] = new StringBuilder();
    }
  }

  private void AccumulateToolCallData(
      StreamingState state,
      int index,
      StreamingChatToolCallUpdate toolCallUpdate)
  {
    // Accumulate tool call ID
    if (!string.IsNullOrEmpty(toolCallUpdate.ToolCallId))
    {
      if (!state.ToolCallsInfo.ContainsKey(index))
      {
        state.ToolCallsInfo[index] = (toolCallUpdate.ToolCallId, string.Empty);
      }
    }

    // Accumulate function name
    if (!string.IsNullOrEmpty(toolCallUpdate.FunctionName))
    {
      var current = state.ToolCallsInfo.GetValueOrDefault(index, (string.Empty, string.Empty));
      state.ToolCallsInfo[index] = (current.Item1, toolCallUpdate.FunctionName);
    }

    // Accumulate function arguments
    if (toolCallUpdate.FunctionArgumentsUpdate != null &&
        toolCallUpdate.FunctionArgumentsUpdate.ToMemory().Length > 0)
    {
      state.ToolCallsBuilder[index].Append(toolCallUpdate.FunctionArgumentsUpdate.ToString());
    }
  }

  private async Task<(string response, int totalTokenUsed)> ExecuteToolsAndGetFinalResponseAsync(
    List<OpenAI.Chat.ChatMessage> messages,
    StreamingState state,
    ChatCompletionOptions chatOptions,
    Func<string, Task>? onChunkReceived,
    CancellationToken cancellationToken)
  {
    logger.LogInformation($"LLM requested {state.ToolCallsInfo.Count} tool calls during streaming");

    // Build and execute tool calls
    var toolCalls = BuildToolCallsFromState(state);
    messages.Add(new AssistantChatMessage(toolCalls));

    foreach (var toolCall in toolCalls)
    {
      await ExecuteToolAndAddResultMessage(messages, toolCall);
    }

    // Stream final response after tool execution
    state.ResponseBuilder.Clear();
    try
    {
      await foreach (var update in chatClient.CompleteChatStreamingAsync(messages, chatOptions, cancellationToken))
      {
        await ProcessStreamUpdate(update, state, onChunkReceived);
      }
    }
    catch (OperationCanceledException)
    {
      logger.LogWarning($"Final streaming cancelled after tools - returning partial content ({state.ResponseBuilder.Length} chars)");
    }

    return (state.ResponseBuilder.ToString(), state.TotalTokensUsed);
  }

  private List<ChatToolCall> BuildToolCallsFromState(StreamingState state)
  {
    var toolCalls = new List<ChatToolCall>();

    foreach (var kvp in state.ToolCallsInfo)
    {
      int index = kvp.Key;
      var (id, functionName) = kvp.Value;
      var arguments = state.ToolCallsBuilder[index].ToString();

      toolCalls.Add(ChatToolCall.CreateFunctionToolCall(id, functionName, BinaryData.FromString(arguments)));
    }

    return toolCalls;
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
      var systemMessage = $"Reference Content:\n{relatedNewsSourceContent}\n\n" +
                       $"Use this content to provide accurate answers based on the source material.";
      messages.Add(new SystemChatMessage(systemMessage));
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
    { }
    return "{}";
  }

  private ChatCompletionOptions CreateChatOptionsWithTools(IList<McpClientTool> availableTools)
  {
    var chatOptions = new ChatCompletionOptions
    {
      MaxOutputTokenCount = settings.MaxToken
    };

    foreach (var tool in availableTools)
    {
      var chatTool = ConvertMcpToolToChatTool(tool);
      if (chatTool != null)
      {
        chatOptions.Tools.Add(chatTool);
      }
    }

    return chatOptions;
  }

  #endregion

  #region Helper Classes

  private class StreamingState
  {
    public StringBuilder ResponseBuilder { get; } = new();
    public int TotalTokensUsed { get; set; } = 0;
    public bool RequiresToolExecution { get; set; } = false;
    public Dictionary<int, StringBuilder> ToolCallsBuilder { get; } = new();
    public Dictionary<int, (string id, string functionName)> ToolCallsInfo { get; } = new();
  }

  #endregion
}
