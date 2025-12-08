using System.Net;
using System.Net.Http.Json;
using System.Text;
using FluentAssertions;
using Xunit;
using backend.E2ETests.Helpers;
using Microsoft.AspNetCore.SignalR.Client;
using backend.Models.Dtos;
using backend.Models.Domain;

namespace backend.E2ETests.Flows;

/// <summary>
/// E2E tests for SignalR real-time chat streaming functionality.
/// Tests the complete flow of sending messages and receiving AI-streamed responses.
/// </summary>
public class SignalRStreamingTests : IClassFixture<E2EWebAppFactory>
{
  private readonly E2EWebAppFactory _factory;

  public SignalRStreamingTests(E2EWebAppFactory factory)
  {
    _factory = factory;
  }

  /// <summary>
  /// Test complete chat flow: Create chat → Connect SignalR → Join chat → Send message → Receive streamed chunks → Receive final message
  /// Note: Skipped because InMemory DB doesn't support ExecuteUpdate - would need real database
  /// </summary>
  [Fact(Skip = "Requires real OpenAI API - mock service doesn't support streaming")]
  public async Task CompleteChatFlow_WithSignalRStreaming_Success()
  {
    // Arrange - Create authenticated user and chat
    var (client, email, _, userId) = await TestUserHelper.CreateAuthenticatedUserAsync(_factory);

    var createChatDto = new CreateChatDTO { FirstUserMessage = "Hello, what is 2+2?" };
    var createResponse = await client.PostAsJsonAsync("/chat", createChatDto);
    createResponse.StatusCode.Should().Be(HttpStatusCode.OK);
    var chat = await createResponse.Content.ReadFromJsonAsync<ChatResponse>();
    chat.Should().NotBeNull();

    // Extract Bearer token from client
    var token = client.DefaultRequestHeaders.Authorization?.Parameter;
    token.Should().NotBeNullOrEmpty();

    // Setup SignalR connection with Bearer token
    var hubConnection = new HubConnectionBuilder()
      .WithUrl($"http://localhost/chathub", options =>
      {
        options.HttpMessageHandlerFactory = _ => _factory.Server.CreateHandler();
        options.AccessTokenProvider = () => Task.FromResult(token)!;
      })
      .WithAutomaticReconnect()
      .Build();

    var receivedChunks = new List<string>();
    var receivedFinalMessage = new TaskCompletionSource<FullMessageDto>();
    var chatJoinedTcs = new TaskCompletionSource<bool>();

    hubConnection.On<object>("ReceiveMessage", (message) =>
    {
      if (message is ChunkMessageDto chunk)
      {
        receivedChunks.Add(chunk.Content);
      }
      else if (message is FullMessageDto fullMessage)
      {
        receivedFinalMessage.TrySetResult(fullMessage);
      }
    });

    hubConnection.On<string, string, List<FullMessageDto>>("ChatJoined", (chatId, title, messages) =>
    {
      chatJoinedTcs.TrySetResult(true);
    });

    // Act - Connect and join chat
    await hubConnection.StartAsync();
    await hubConnection.InvokeAsync("JoinChat", chat!.Id.ToString());
    await chatJoinedTcs.Task.WaitAsync(TimeSpan.FromSeconds(5));

    // Send message and wait for streaming
    await hubConnection.InvokeAsync("SendMessage", chat.Id.ToString(), "Explain what TypeScript is");

    // Wait for final message (with timeout)
    var finalMessage = await receivedFinalMessage.Task.WaitAsync(TimeSpan.FromSeconds(30));

    // Assert
    receivedChunks.Should().NotBeEmpty("should receive streaming chunks");
    finalMessage.Should().NotBeNull();
    finalMessage.Type.Should().Be(MessageType.Assistant);
    finalMessage.Content.Should().NotBeNullOrEmpty();
    finalMessage.IsChunkMessage.Should().BeFalse();

    // Cleanup
    await hubConnection.StopAsync();
    await hubConnection.DisposeAsync();
  }

  /// <summary>
  /// Test StopGeneration cancels streaming during AI response
  /// Note: Skipped because InMemory DB doesn't support ExecuteUpdate - would need real database
  /// </summary>
  [Fact(Skip = "Requires real OpenAI API streaming - mock service doesn't support cancellation")]
  public async Task SendMessage_ThenStopGeneration_CancelsStreaming()
  {
    // Arrange
    var (client, _, _, _) = await TestUserHelper.CreateAuthenticatedUserAsync(_factory);

    var createChatDto = new CreateChatDTO { FirstUserMessage = "Start chat" };
    var createResponse = await client.PostAsJsonAsync("/chat", createChatDto);
    var chat = await createResponse.Content.ReadFromJsonAsync<ChatResponse>();

    var token = client.DefaultRequestHeaders.Authorization?.Parameter;

    var hubConnection = new HubConnectionBuilder()
      .WithUrl($"http://localhost/chathub", options =>
      {
        options.HttpMessageHandlerFactory = _ => _factory.Server.CreateHandler();
        options.AccessTokenProvider = () => Task.FromResult(token)!;
      })
      .Build();

    var receivedChunks = new List<string>();
    var chatJoinedTcs = new TaskCompletionSource<bool>();

    hubConnection.On<object>("ReceiveMessage", (message) =>
    {
      if (message is ChunkMessageDto chunk)
      {
        receivedChunks.Add(chunk.Content);
      }
    });

    hubConnection.On<string, string, List<FullMessageDto>>("ChatJoined", (chatId, title, messages) =>
    {
      chatJoinedTcs.TrySetResult(true);
    });

    await hubConnection.StartAsync();
    await hubConnection.InvokeAsync("JoinChat", chat!.Id.ToString());
    await chatJoinedTcs.Task.WaitAsync(TimeSpan.FromSeconds(5));

    // Act - Send message and immediately stop
    await hubConnection.InvokeAsync("SendMessage", chat.Id.ToString(), "Write a very long essay about the history of computers");
    await Task.Delay(100); // Wait for streaming to start
    await hubConnection.InvokeAsync("StopGeneration", chat.Id.ToString());

    await Task.Delay(500); // Wait to ensure no more chunks arrive

    // Assert - Should have received some chunks but not complete response
    receivedChunks.Should().NotBeEmpty("should receive at least some chunks before cancellation");

    // Cleanup
    await hubConnection.StopAsync();
    await hubConnection.DisposeAsync();
  }

  /// <summary>
  /// Test that empty message is rejected
  /// </summary>
  [Fact]
  public async Task SendMessage_WithEmptyContent_ReturnsError()
  {
    // Arrange
    var (client, _, _, _) = await TestUserHelper.CreateAuthenticatedUserAsync(_factory);

    var createChatDto = new CreateChatDTO { FirstUserMessage = "Initial message" };
    var createResponse = await client.PostAsJsonAsync("/chat", createChatDto);
    var chat = await createResponse.Content.ReadFromJsonAsync<ChatResponse>();

    var token = client.DefaultRequestHeaders.Authorization?.Parameter;

    var hubConnection = new HubConnectionBuilder()
      .WithUrl($"http://localhost/chathub", options =>
      {
        options.HttpMessageHandlerFactory = _ => _factory.Server.CreateHandler();
        options.AccessTokenProvider = () => Task.FromResult(token)!;
      })
      .Build();

    var errorReceived = new TaskCompletionSource<string>();
    var chatJoinedTcs = new TaskCompletionSource<bool>();

    hubConnection.On<string>("Error", (error) =>
    {
      errorReceived.TrySetResult(error);
    });

    hubConnection.On<string, string, List<FullMessageDto>>("ChatJoined", (chatId, title, messages) =>
    {
      chatJoinedTcs.TrySetResult(true);
    });

    await hubConnection.StartAsync();
    await hubConnection.InvokeAsync("JoinChat", chat!.Id.ToString());
    await chatJoinedTcs.Task.WaitAsync(TimeSpan.FromSeconds(5));

    // Act - Send empty message
    await hubConnection.InvokeAsync("SendMessage", chat.Id.ToString(), "   ");

    // Wait for error
    var error = await errorReceived.Task.WaitAsync(TimeSpan.FromSeconds(5));

    // Assert
    error.Should().Contain("empty", "should indicate message is empty");

    // Cleanup
    await hubConnection.StopAsync();
    await hubConnection.DisposeAsync();
  }

  /// <summary>
  /// Test that user cannot access chat they don't own
  /// </summary>
  [Fact]
  public async Task JoinChat_WithUnauthorizedChatId_ReturnsError()
  {
    // Arrange - Create two users
    var (client1, _, _, _) = await TestUserHelper.CreateAuthenticatedUserAsync(_factory);
    var (client2, _, _, _) = await TestUserHelper.CreateAuthenticatedUserAsync(_factory);

    // User 1 creates a chat
    var createChatDto = new CreateChatDTO { FirstUserMessage = "My private chat" };
    var createResponse = await client1.PostAsJsonAsync("/chat", createChatDto);
    var chat = await createResponse.Content.ReadFromJsonAsync<ChatResponse>();

    // User 2 tries to join
    var token2 = client2.DefaultRequestHeaders.Authorization?.Parameter;

    var hubConnection = new HubConnectionBuilder()
      .WithUrl($"http://localhost/chathub", options =>
      {
        options.HttpMessageHandlerFactory = _ => _factory.Server.CreateHandler();
        options.AccessTokenProvider = () => Task.FromResult(token2)!;
      })
      .Build();

    var errorReceived = new TaskCompletionSource<string>();

    hubConnection.On<string>("Error", (error) =>
    {
      errorReceived.TrySetResult(error);
    });

    await hubConnection.StartAsync();

    // Act - Try to join unauthorized chat
    await hubConnection.InvokeAsync("JoinChat", chat!.Id.ToString());

    // Wait for error
    var error = await errorReceived.Task.WaitAsync(TimeSpan.FromSeconds(5));

    // Assert
    error.Should().Contain("not found", "should indicate chat access denied");

    // Cleanup
    await hubConnection.StopAsync();
    await hubConnection.DisposeAsync();
  }

  #region DTOs

  public class CreateChatDTO
  {
    public string FirstUserMessage { get; set; } = string.Empty;
  }

  public class ChatResponse
  {
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
  }

  public class ChunkMessageDto
  {
    public string Content { get; set; } = string.Empty;
    public bool IsChunkMessage { get; set; } = true;
  }

  public class FullMessageDto
  {
    public Guid Id { get; set; }
    public string Content { get; set; } = string.Empty;
    public MessageType Type { get; set; }
    public DateTime CreatedAt { get; set; }
    public int TokenCount { get; set; }
    public bool IsStarred { get; set; }
    public bool IsReported { get; set; }
    public bool IsChunkMessage { get; set; }
  }

  #endregion
}
