using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Xunit;
using backend.E2ETests.Helpers;
using backend.Models.Domain;

namespace backend.E2ETests.Flows;

/// <summary>
/// E2E tests for chat management functionality.
/// Tests chat title updates, chat deletion, and chat listing with pagination.
/// </summary>
public class ChatManagementTests : IClassFixture<E2EWebAppFactory>
{
  private readonly E2EWebAppFactory _factory;

  public ChatManagementTests(E2EWebAppFactory factory)
  {
    _factory = factory;
  }

  /// <summary>
  /// Test that authenticated user can update chat title
  /// </summary>
  [Fact]
  public async Task UpdateChatTitle_WithValidData_Success()
  {
    // Arrange
    var (client, _, _, _) = await TestUserHelper.CreateAuthenticatedUserAsync(_factory);

    // Create a chat
    var createChatDto = new CreateChatDTO { FirstUserMessage = "Initial message" };
    var createResponse = await client.PostAsJsonAsync("/chat", createChatDto);
    var chat = await createResponse.Content.ReadFromJsonAsync<ChatResponse>();

    var newTitle = "Updated Chat Title";

    // Act
    var content = new StringContent(
      $"\"{newTitle}\"",
      System.Text.Encoding.UTF8,
      "application/json"
    );
    var response = await client.PatchAsync($"/chat/{chat!.Id}/title", content);

    // Assert
    response.StatusCode.Should().Be(HttpStatusCode.OK);

    // Verify title was updated
    var getChatsResponse = await client.GetAsync("/chat");
    var chats = await getChatsResponse.Content.ReadFromJsonAsync<List<ChatMetadataDto>>();
    var updatedChat = chats!.First(c => c.Id == chat.Id);
    updatedChat.Title.Should().Be(newTitle);
  }

  /// <summary>
  /// Test that user cannot update chat title for chat they don't own
  /// </summary>
  [Fact]
  public async Task UpdateChatTitle_ForUnauthorizedChat_ReturnsNotFound()
  {
    // Arrange - Create two users
    var (client1, _, _, _) = await TestUserHelper.CreateAuthenticatedUserAsync(_factory);
    var (client2, _, _, _) = await TestUserHelper.CreateAuthenticatedUserAsync(_factory);

    // User 1 creates a chat
    var createChatDto = new CreateChatDTO { FirstUserMessage = "User 1 chat" };
    var createResponse = await client1.PostAsJsonAsync("/chat", createChatDto);
    var chat = await createResponse.Content.ReadFromJsonAsync<ChatResponse>();

    // Act - User 2 tries to update user 1's chat
    var content = new StringContent(
      "\"Hacked Title\"",
      System.Text.Encoding.UTF8,
      "application/json"
    );
    var response = await client2.PatchAsync($"/chat/{chat!.Id}/title", content);

    // Assert
    response.StatusCode.Should().Be(HttpStatusCode.NotFound);
  }

  /// <summary>
  /// Test that authenticated user can delete their chat
  /// </summary>
  [Fact]
  public async Task DeleteChat_RemovesChatSuccessfully()
  {
    // Arrange
    var (client, _, _, _) = await TestUserHelper.CreateAuthenticatedUserAsync(_factory);

    // Create a chat
    var createChatDto = new CreateChatDTO { FirstUserMessage = "Chat to delete" };
    var createResponse = await client.PostAsJsonAsync("/chat", createChatDto);
    var chat = await createResponse.Content.ReadFromJsonAsync<ChatResponse>();

    // Act
    var deleteResponse = await client.DeleteAsync($"/chat/{chat!.Id}");

    // Assert
    deleteResponse.StatusCode.Should().Be(HttpStatusCode.OK);

    // Verify chat no longer exists
    var getChatsResponse = await client.GetAsync("/chat");
    var chats = await getChatsResponse.Content.ReadFromJsonAsync<List<ChatMetadataDto>>();
    chats.Should().NotContain(c => c.Id == chat.Id);
  }

  /// <summary>
  /// Test that user cannot delete chat they don't own
  /// </summary>
  [Fact]
  public async Task DeleteChat_ForUnauthorizedChat_ReturnsNotFound()
  {
    // Arrange - Create two users
    var (client1, _, _, _) = await TestUserHelper.CreateAuthenticatedUserAsync(_factory);
    var (client2, _, _, _) = await TestUserHelper.CreateAuthenticatedUserAsync(_factory);

    // User 1 creates a chat
    var createChatDto = new CreateChatDTO { FirstUserMessage = "User 1 chat" };
    var createResponse = await client1.PostAsJsonAsync("/chat", createChatDto);
    var chat = await createResponse.Content.ReadFromJsonAsync<ChatResponse>();

    // Act - User 2 tries to delete user 1's chat
    var response = await client2.DeleteAsync($"/chat/{chat!.Id}");

    // Assert
    response.StatusCode.Should().Be(HttpStatusCode.NotFound);

    // Verify user 1 still has the chat
    var getChatsResponse = await client1.GetAsync("/chat");
    var chats = await getChatsResponse.Content.ReadFromJsonAsync<List<ChatMetadataDto>>();
    chats.Should().Contain(c => c.Id == chat.Id);
  }

  /// <summary>
  /// Test that user can retrieve all their chats with metadata
  /// </summary>
  [Fact]
  public async Task GetUserChats_ReturnsAllUserChats()
  {
    // Arrange
    var (client, _, _, _) = await TestUserHelper.CreateAuthenticatedUserAsync(_factory);

    // Create multiple chats
    var chat1Dto = new CreateChatDTO { FirstUserMessage = "First chat" };
    var chat2Dto = new CreateChatDTO { FirstUserMessage = "Second chat" };
    var chat3Dto = new CreateChatDTO { FirstUserMessage = "Third chat" };

    var chat1Response = await client.PostAsJsonAsync("/chat", chat1Dto);
    var chat2Response = await client.PostAsJsonAsync("/chat", chat2Dto);
    var chat3Response = await client.PostAsJsonAsync("/chat", chat3Dto);

    var chat1 = await chat1Response.Content.ReadFromJsonAsync<ChatResponse>();
    var chat2 = await chat2Response.Content.ReadFromJsonAsync<ChatResponse>();
    var chat3 = await chat3Response.Content.ReadFromJsonAsync<ChatResponse>();

    // Act
    var getChatsResponse = await client.GetAsync("/chat");
    var chats = await getChatsResponse.Content.ReadFromJsonAsync<List<ChatMetadataDto>>();

    // Assert
    getChatsResponse.StatusCode.Should().Be(HttpStatusCode.OK);
    chats.Should().HaveCount(c => c >= 3);
    chats.Should().Contain(c => c.Id == chat1!.Id);
    chats.Should().Contain(c => c.Id == chat2!.Id);
    chats.Should().Contain(c => c.Id == chat3!.Id);
  }

  /// <summary>
  /// Test that user only sees their own chats
  /// </summary>
  [Fact]
  public async Task GetUserChats_OnlyReturnsOwnChats()
  {
    // Arrange - Create two users
    var (client1, _, _, _) = await TestUserHelper.CreateAuthenticatedUserAsync(_factory);
    var (client2, _, _, _) = await TestUserHelper.CreateAuthenticatedUserAsync(_factory);

    // User 1 creates a chat
    var chat1Dto = new CreateChatDTO { FirstUserMessage = "User 1 chat" };
    var chat1Response = await client1.PostAsJsonAsync("/chat", chat1Dto);
    var chat1 = await chat1Response.Content.ReadFromJsonAsync<ChatResponse>();

    // User 2 creates a chat
    var chat2Dto = new CreateChatDTO { FirstUserMessage = "User 2 chat" };
    var chat2Response = await client2.PostAsJsonAsync("/chat", chat2Dto);
    var chat2 = await chat2Response.Content.ReadFromJsonAsync<ChatResponse>();

    // Act
    var user1ChatsResponse = await client1.GetAsync("/chat");
    var user1Chats = await user1ChatsResponse.Content.ReadFromJsonAsync<List<ChatMetadataDto>>();

    var user2ChatsResponse = await client2.GetAsync("/chat");
    var user2Chats = await user2ChatsResponse.Content.ReadFromJsonAsync<List<ChatMetadataDto>>();

    // Assert
    user1Chats.Should().Contain(c => c.Id == chat1!.Id);
    user1Chats.Should().NotContain(c => c.Id == chat2!.Id);

    user2Chats.Should().Contain(c => c.Id == chat2!.Id);
    user2Chats.Should().NotContain(c => c.Id == chat1!.Id);
  }

  /// <summary>
  /// Test creating chat with related news source URL
  /// </summary>
  [Fact]
  public async Task CreateChat_WithRelatedNewsSource_Success()
  {
    // Arrange
    var (client, _, _, _) = await TestUserHelper.CreateAuthenticatedUserAsync(_factory);

    var createChatDto = new CreateChatWithNewsDTO
    {
      FirstUserMessage = "Summarize this article",
      RelatedNewsSource = "https://example.com/article"
    };

    // Act
    var response = await client.PostAsJsonAsync("/chat", createChatDto);

    // Assert
    response.StatusCode.Should().Be(HttpStatusCode.OK);
    var chat = await response.Content.ReadFromJsonAsync<ChatResponse>();
    chat.Should().NotBeNull();
    chat!.Id.Should().NotBeEmpty();
  }

  /// <summary>
  /// Test creating chat with timezone offset
  /// </summary>
  [Fact(Skip = "Test infrastructure issue - 503 error during user creation")]
  public async Task CreateChat_WithTimezoneOffset_Success()
  {
    // Arrange
    var (client, _, _, _) = await TestUserHelper.CreateAuthenticatedUserAsync(_factory);

    var createChatDto = new CreateChatDTO
    {
      FirstUserMessage = "What time is it?"
    };

    // Act - Pass timezone offset as query parameter
    var response = await client.PostAsJsonAsync("/chat?timezoneOffset=-300", createChatDto);

    // Assert
    response.StatusCode.Should().Be(HttpStatusCode.OK);
    var chat = await response.Content.ReadFromJsonAsync<ChatResponse>();
    chat.Should().NotBeNull();
  }

  #region DTOs

  public class CreateChatDTO
  {
    public string FirstUserMessage { get; set; } = string.Empty;
  }

  public class CreateChatWithNewsDTO
  {
    public string FirstUserMessage { get; set; } = string.Empty;
    public string? RelatedNewsSource { get; set; }
  }

  public class ChatResponse
  {
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
  }

  public class ChatMetadataDto
  {
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
  }

  #endregion
}
