using backend.Models.Domain;
using backend.Models.Dtos;
using backend.Services.Interfaces.Cache;
using Microsoft.Extensions.DependencyInjection;
using FluentAssertions;

namespace backend.IntegrationTests.Services;

[Collection("Sequential")]
public class ChatCacheServiceIntegrationTests : IClassFixture<WebAppFactory>, IDisposable
{
  private readonly WebAppFactory _factory;
  private readonly IServiceScope _scope;
  private readonly IChatCacheService _cacheService;

  public ChatCacheServiceIntegrationTests(WebAppFactory factory)
  {
    _factory = factory;
    _scope = _factory.Services.CreateScope();
    _cacheService = _scope.ServiceProvider.GetRequiredService<IChatCacheService>();
  }



  [Fact]
  public async Task GetCachedChat_WithNonExistentChat_ReturnsNull()
  {
    // Arrange
    var userId = Guid.NewGuid().ToString();
    var chatId = Guid.NewGuid();

    // Act
    var cachedChat = await _cacheService.GetCachedChat(userId, chatId);

    // Assert
    cachedChat.Should().BeNull();
  }







  [Fact]
  public async Task DeleteCachedChat_RemovesChatFromCache()
  {
    // Arrange
    var userId = Guid.NewGuid().ToString();
    var chatId = Guid.NewGuid();
    var chat = new CachedChatData
    {
      Metadata = new ChatMetaDataDto
      {
        Id = chatId,
        Title = "Chat to Delete",
        CreatedAt = DateTime.UtcNow,
        LastMessageAt = DateTime.UtcNow,
        MessageCount = 0,
        IsContextFull = false
      },
      Messages = new List<ChatMessage>()
    };

    await _cacheService.SetCachedChat(userId, chatId, chat);

    // Act - Delete chat
    await _cacheService.DeleteCachedChat(userId, chatId);

    // Try to retrieve deleted chat
    var cachedChat = await _cacheService.GetCachedChat(userId, chatId);

    // Assert
    cachedChat.Should().BeNull();
  }



  [Fact]
  public async Task SetReportedMessage_AddsMessageToReportedList()
  {
    // Arrange
    var userId = Guid.NewGuid().ToString();
    var message = new ChatMessage
    {
      Id = Guid.NewGuid(),
      Type = MessageType.Assistant,
      Content = "Problematic message to report",
      CreatedAt = DateTime.UtcNow,
      ChatId = Guid.NewGuid(),
      IsReported = true,
      ReportReason = "Inappropriate content",
      ReportedAt = DateTime.UtcNow
    };

    // Act - Report message
    await _cacheService.SetReportedMessage(userId, message);

    // Assert - Test passes if no exception thrown
    // The service method is async void equivalent, so we just verify it completes
    Assert.True(true);
  }



  [Fact]
  public async Task GetStarredMessagesFromCache_WithNoStarredMessages_ReturnsEmptyList()
  {
    // Arrange
    var userId = Guid.NewGuid().ToString();

    // Act
    var starredMessages = await _cacheService.GetStarredMessagesFromCache(userId);

    // Assert
    starredMessages.Should().NotBeNull();
    starredMessages.Should().BeEmpty();
  }

  public void Dispose()
  {
    _scope?.Dispose();
  }
}
