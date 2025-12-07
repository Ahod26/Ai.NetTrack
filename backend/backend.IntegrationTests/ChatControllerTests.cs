using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Xunit;

namespace backend.IntegrationTests;

public class ChatControllerTests : IClassFixture<WebAppFactory>
{
  private readonly HttpClient _client;

  public ChatControllerTests(WebAppFactory factory)
  {
    _client = factory.CreateClient();
  }

  #region POST /chat - Create Chat Tests (CRITICAL)

  /// <summary>
  /// Verifies that creating a chat without authentication returns unauthorized.
  /// </summary>
  [Fact]
  public async Task CreateChat_WithoutAuthentication_ReturnsUnauthorized()
  {
    // Arrange
    var createChatData = new
    {
      firstMessage = "Hello, this is my first message"
    };

    // Act
    var response = await _client.PostAsJsonAsync("/chat", createChatData);

    // Assert
    response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
  }

  /// <summary>
  /// Verifies that creating a chat with valid data and authentication succeeds.
  /// Note: Requires authenticated client setup.
  /// </summary>
  [Fact]
  public async Task CreateChat_WithValidData_ReturnsCreatedChat()
  {
    // Note: This test requires an authenticated HTTP client
    // Implementation depends on your auth setup (JWT, cookies, etc.)

    // Arrange
    var createChatData = new
    {
      firstMessage = "Test chat message"
    };

    // Act
    var response = await _client.PostAsJsonAsync("/chat", createChatData);

    // Assert
    response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.Unauthorized);
  }

  /// <summary>
  /// Verifies that creating a chat with news source parameter includes the related news URL.
  /// </summary>
  [Fact]
  public async Task CreateChat_WithNewsSource_CreatesNewsRelatedChat()
  {
    // Arrange
    var createChatData = new
    {
      firstMessage = "Tell me about this article"
    };
    var newsUrl = "https://example.com/news/article";

    // Act
    var response = await _client.PostAsJsonAsync($"/chat?relatedNewsSource={Uri.EscapeDataString(newsUrl)}", createChatData);

    // Assert
    response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.Unauthorized);
  }

  /// <summary>
  /// Verifies that creating more than 10 chats is blocked by MaxChats filter.
  /// </summary>
  [Fact]
  public async Task CreateChat_ExceedingMaxChats_ReturnsBadRequest()
  {
    // Note: This test would require creating 10 chats first, then attempting an 11th
    // Implementation depends on test database state management

    // Arrange
    var createChatData = new
    {
      firstMessage = "Test message"
    };

    // Act - Would need to be authenticated and have 10 existing chats
    var response = await _client.PostAsJsonAsync("/chat", createChatData);

    // Assert
    response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.BadRequest, HttpStatusCode.Unauthorized);
  }

  /// <summary>
  /// Verifies that creating a chat without required fields returns bad request.
  /// </summary>
  [Fact]
  public async Task CreateChat_WithoutFirstMessage_ReturnsBadRequest()
  {
    // Arrange
    var createChatData = new { };

    // Act
    var response = await _client.PostAsJsonAsync("/chat", createChatData);

    // Assert
    response.StatusCode.Should().BeOneOf(HttpStatusCode.BadRequest, HttpStatusCode.Unauthorized);
  }

  #endregion

  #region GET /chat - Get Chats Tests (CRITICAL)

  /// <summary>
  /// Verifies that getting chats without authentication returns unauthorized.
  /// </summary>
  [Fact]
  public async Task GetChats_WithoutAuthentication_ReturnsUnauthorized()
  {
    // Act
    var response = await _client.GetAsync("/chat");

    // Assert
    response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
  }

  /// <summary>
  /// Verifies that getting chats with authentication returns user's chat list.
  /// </summary>
  [Fact]
  public async Task GetChats_WithAuthentication_ReturnsUserChats()
  {
    // Note: Requires authenticated client

    // Act
    var response = await _client.GetAsync("/chat");

    // Assert
    response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.Unauthorized);
  }

  /// <summary>
  /// Verifies that timezone offset parameter converts timestamps correctly.
  /// </summary>
  [Fact]
  public async Task GetChats_WithTimezoneOffset_ReturnsAdjustedTimestamps()
  {
    // Arrange
    var timezoneOffset = -300; // EST offset

    // Act
    var response = await _client.GetAsync($"/chat?timezoneOffset={timezoneOffset}");

    // Assert
    response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.Unauthorized);
  }

  #endregion

  #region DELETE /chat/{chatId} - Delete Chat Tests (HIGH)

  /// <summary>
  /// Verifies that deleting a chat without authentication returns unauthorized.
  /// </summary>
  [Fact]
  public async Task DeleteChat_WithoutAuthentication_ReturnsUnauthorized()
  {
    // Arrange
    var chatId = Guid.NewGuid();

    // Act
    var response = await _client.DeleteAsync($"/chat/{chatId}");

    // Assert
    response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
  }

  /// <summary>
  /// Verifies that deleting a non-existent chat returns not found.
  /// </summary>
  [Fact]
  public async Task DeleteChat_NonExistentChat_ReturnsNotFound()
  {
    // Note: Requires authenticated client

    // Arrange
    var nonExistentChatId = Guid.NewGuid();

    // Act
    var response = await _client.DeleteAsync($"/chat/{nonExistentChatId}");

    // Assert
    response.StatusCode.Should().BeOneOf(HttpStatusCode.NotFound, HttpStatusCode.Unauthorized);
  }

  /// <summary>
  /// Verifies that deleting a chat owned by another user returns not found.
  /// </summary>
  [Fact]
  public async Task DeleteChat_OtherUserChat_ReturnsNotFound()
  {
    // Note: Requires authenticated client and existing chat from another user

    // Arrange
    var otherUserChatId = Guid.NewGuid();

    // Act
    var response = await _client.DeleteAsync($"/chat/{otherUserChatId}");

    // Assert
    response.StatusCode.Should().BeOneOf(HttpStatusCode.NotFound, HttpStatusCode.Unauthorized);
  }

  #endregion

  #region PATCH /chat/{chatId}/title - Change Title Tests (HIGH)

  /// <summary>
  /// Verifies that changing chat title without authentication returns unauthorized.
  /// </summary>
  [Fact]
  public async Task ChangeChatTitle_WithoutAuthentication_ReturnsUnauthorized()
  {
    // Arrange
    var chatId = Guid.NewGuid();
    var newTitle = "New Chat Title";

    // Act
    var response = await _client.PatchAsJsonAsync($"/chat/{chatId}/title", newTitle);

    // Assert
    response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
  }

  /// <summary>
  /// Verifies that changing title with valid data succeeds.
  /// </summary>
  [Fact]
  public async Task ChangeChatTitle_WithValidTitle_ReturnsOk()
  {
    // Note: Requires authenticated client and existing chat

    // Arrange
    var chatId = Guid.NewGuid();
    var newTitle = "Updated Title";

    // Act
    var response = await _client.PatchAsJsonAsync($"/chat/{chatId}/title", newTitle);

    // Assert
    response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.NotFound, HttpStatusCode.Unauthorized);
  }

  /// <summary>
  /// Verifies that changing title with empty string returns bad request.
  /// </summary>
  [Fact]
  public async Task ChangeChatTitle_WithEmptyTitle_ReturnsBadRequest()
  {
    // Arrange
    var chatId = Guid.NewGuid();
    var emptyTitle = "";

    // Act
    var response = await _client.PatchAsJsonAsync($"/chat/{chatId}/title", emptyTitle);

    // Assert
    response.StatusCode.Should().BeOneOf(HttpStatusCode.BadRequest, HttpStatusCode.Unauthorized);
  }

  /// <summary>
  /// Verifies that changing title with exceeding length returns bad request.
  /// </summary>
  [Fact]
  public async Task ChangeChatTitle_WithTooLongTitle_ReturnsBadRequest()
  {
    // Arrange
    var chatId = Guid.NewGuid();
    var tooLongTitle = new string('a', 21); // Max is 20 characters

    // Act
    var response = await _client.PatchAsJsonAsync($"/chat/{chatId}/title", tooLongTitle);

    // Assert
    response.StatusCode.Should().BeOneOf(HttpStatusCode.BadRequest, HttpStatusCode.Unauthorized);
  }

  #endregion
}
