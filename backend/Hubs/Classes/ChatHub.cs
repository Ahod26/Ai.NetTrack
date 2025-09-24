using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using backend.Models.Dtos;
using backend.Hubs.Interfaces;
using backend.Services.Interfaces.Chat;
using System.Collections.Concurrent;


namespace backend.Hubs.Classes;

[Authorize]
public class ChatHub(IChatService chatService,
ILogger<ChatHub> logger,
IHubContext<ChatHub, IChatClient> hubContext,
IServiceScopeFactory scopeFactory) : Hub<IChatClient>
{
  private static readonly ConcurrentDictionary<string, CancellationTokenSource> _generationTokens = new();
  private static readonly ConcurrentDictionary<string, bool> _pendingStops = new();

  private string GetKey(string userId, string chatId) => $"{userId}:{chatId}";

  public async Task JoinChat(string chatId)
  {
    var userId = Context.UserIdentifier!;

    var chat = await chatService.GetUserChatAsync(Guid.Parse(chatId), userId);
    if (chat == null)
    {
      await Clients.Caller.Error("Chat not found");
      return;
    }

    await Groups.AddToGroupAsync(Context.ConnectionId, $"Chat_{chatId}");

    var messages = await chatService.GetAllChatMessagesAsync(Guid.Parse(chatId), userId);
    await Clients.Caller.ChatJoined(chatId, chat.Title, messages ?? new List<FullMessageDto>());
  }

  public Task SendMessage(string chatId, string content)
  {
    var userId = Context.UserIdentifier!;

    // Kick off processing in the background so this hub connection can accept StopGeneration concurrently
    _ = Task.Run(async () =>
    {
      // Copy DI services into locals to avoid capturing hub instance
      var _logger = logger;
      var _hub = hubContext;
      var _scopeFactory = scopeFactory;
      var _userId = userId;
      var _chatId = chatId;
      var _content = content;

      try
      {
        // Create a new scope for scoped services (e.g., DbContext via repos/services)
        using var scope = _scopeFactory.CreateScope();
        var _chatService = scope.ServiceProvider.GetRequiredService<IChatService>();

        // Validate chat and content first (using service/repo)
        var chat = await _chatService.GetUserChatAsync(Guid.Parse(_chatId), _userId);
        if (chat == null)
        {
          await _hub.Clients.User(_userId).Error("Access denied");
          return;
        }

        if (chat.IsContextFull)
        {
          await _hub.Clients.User(_userId).Error("Chat context is full");
          return;
        }

        if (string.IsNullOrWhiteSpace(_content))
        {
          await _hub.Clients.User(_userId).Error("Message cannot be empty");
          return;
        }

        CancellationTokenSource? cts = null;
        var key = GetKey(_userId, _chatId);

        // Ensure any previous generation is canceled
        if (_generationTokens.TryRemove(key, out var existing))
        {
          try { existing.Cancel(); } catch { }
          existing.Dispose();
        }

        cts = new CancellationTokenSource();
        _generationTokens[key] = cts;
        // CTS created for generation

        // Apply any pending stop that arrived before CTS creation
        if (_pendingStops.TryRemove(key, out _))
        {
          try { cts.Cancel(); } catch { }
        }

        try
        {
          var aiMessage = await _chatService.ProcessUserMessageAsync(
              Guid.Parse(_chatId),
              _content,
              _userId,
              cts.Token,
              async (chunk) =>
              {
                await _hub.Clients.Group($"Chat_{_chatId}").ReceiveMessage(new ChunkMessageDto { Content = chunk });
              }
          );

          await _hub.Clients.Group($"Chat_{_chatId}").ReceiveMessage(aiMessage);
          // final message sent
        }
        catch (OperationCanceledException)
        {
          // Swallow cancellation as it's user-initiated
        }
        catch (Exception ex)
        {
          _logger.LogError(ex, "SendMessage failed. User {UserId}, Chat {ChatId}", _userId, _chatId);
          await _hub.Clients.User(_userId).Error("Failed to process message");
        }
        finally
        {
          if (_generationTokens.TryRemove(key, out var current))
          {
            current.Dispose();
            // CTS disposed and removed
          }
          cts?.Dispose();
        }
      }
      catch (Exception ex)
      {
        // Guard against outer Task errors
        logger.LogError(ex, "Unhandled error in SendMessage background task. User {UserId}, Chat {ChatId}", userId, chatId);
      }
    });

    // Return immediately so the hub can accept StopGeneration concurrently
    return Task.CompletedTask;
  }

  public Task StopGeneration(string chatId)
  {
    var userId = Context.UserIdentifier!;
    var key = GetKey(userId, chatId);
    if (_generationTokens.TryRemove(key, out var cts))
    {
      try { cts.Cancel(); } finally { cts.Dispose(); }
    }
    else
    {
      // Mark pending stop if no active CTS
      _pendingStops[key] = true;
    }
    return Task.CompletedTask;
  }
}