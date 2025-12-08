using Xunit;
using Moq;
using FluentAssertions;
using Microsoft.Extensions.Options;
using backend.Services.Classes.Cache;
using backend.Repository.Interfaces;
using backend.Models.Configuration;
using backend.Models.Dtos;
using backend.Models.Domain;

namespace backend.UnitTests.Services.Cache;

public class ChatCacheServiceTests
{
  private readonly Mock<IChatCacheRepo> _mockChatCacheRepo;
  private readonly ChatCacheSettings _settings;
  private readonly ChatCacheService _service;

  public ChatCacheServiceTests()
  {
    _mockChatCacheRepo = new Mock<IChatCacheRepo>();

    _settings = new ChatCacheSettings
    {
      CacheDurationHours = 2
    };

    var options = Options.Create(_settings);

    _service = new ChatCacheService(_mockChatCacheRepo.Object, options);
  }

  #region GetCachedChat Tests

  /// <summary>
  /// Verifies that when chat exists in cache, it returns CachedChatData.
  /// </summary>
  [Fact]
  public async Task GetCachedChat_CacheHit_ReturnsCachedChatData()
  {
    // Arrange
    var userId = "user123";
    var chatId = Guid.NewGuid();
    var expectedKey = $"chat:{userId}:{chatId}";
    var cachedData = CreateMockCachedChatData(chatId, 3);

    _mockChatCacheRepo.Setup(x => x.GetCachedChatAsync(expectedKey))
        .ReturnsAsync(cachedData);

    // Act
    var result = await _service.GetCachedChat(userId, chatId);

    // Assert
    result.Should().NotBeNull();
    result.Should().Be(cachedData);
    result!.Metadata.Should().NotBeNull();
    result.Messages.Should().HaveCount(3);
    _mockChatCacheRepo.Verify(x => x.GetCachedChatAsync(expectedKey), Times.Once);
  }

  /// <summary>
  /// Verifies that when chat doesn't exist in cache, it returns null.
  /// </summary>
  [Fact]
  public async Task GetCachedChat_CacheMiss_ReturnsNull()
  {
    // Arrange
    var userId = "user456";
    var chatId = Guid.NewGuid();
    var expectedKey = $"chat:{userId}:{chatId}";

    _mockChatCacheRepo.Setup(x => x.GetCachedChatAsync(expectedKey))
        .ReturnsAsync((CachedChatData?)null);

    // Act
    var result = await _service.GetCachedChat(userId, chatId);

    // Assert
    result.Should().BeNull();
  }

  #endregion

  #region SetCachedChat Tests

  /// <summary>
  /// Verifies that chat is stored with correct cache key and expiration.
  /// </summary>
  [Fact]
  public async Task SetCachedChat_StoresWithCorrectKeyAndExpiration()
  {
    // Arrange
    var userId = "user123";
    var chatId = Guid.NewGuid();
    var expectedKey = $"chat:{userId}:{chatId}";
    var chatData = CreateMockCachedChatData(chatId, 2);

    string? capturedKey = null;
    TimeSpan? capturedExpiration = null;
    _mockChatCacheRepo.Setup(x => x.SetCachedChatAsync(
            It.IsAny<string>(),
            chatData,
            It.IsAny<TimeSpan>()))
        .Callback<string, CachedChatData, TimeSpan>((key, data, exp) =>
        {
          capturedKey = key;
          capturedExpiration = exp;
        })
        .Returns(Task.CompletedTask);

    // Act
    await _service.SetCachedChat(userId, chatId, chatData);

    // Assert
    _mockChatCacheRepo.Verify(x => x.SetCachedChatAsync(expectedKey, chatData, It.IsAny<TimeSpan>()), Times.Once);
    capturedKey.Should().Be(expectedKey);
    capturedExpiration.Should().NotBeNull();
    capturedExpiration!.Value.TotalHours.Should().Be(2);
  }

  #endregion

  #region AddMessageToCachedChat Tests

  /// <summary>
  /// Verifies that when chat exists in cache, message is added and cache is updated.
  /// </summary>
  [Fact]
  public async Task AddMessageToCachedChat_ChatExists_AddsMessageAndUpdatesCache()
  {
    // Arrange
    var userId = "user123";
    var chatId = Guid.NewGuid();
    var cacheKey = $"chat:{userId}:{chatId}";
    var existingChat = CreateMockCachedChatData(chatId, 2);
    var newMessage = new ChatMessage
    {
      Id = Guid.NewGuid(),
      Content = "New message",
      Type = MessageType.User,
      ChatId = chatId
    };

    _mockChatCacheRepo.Setup(x => x.GetCachedChatAsync(cacheKey))
        .ReturnsAsync(existingChat);

    _mockChatCacheRepo.Setup(x => x.UpdateCachedChatAsync(cacheKey, existingChat, It.IsAny<TimeSpan>()))
        .Returns(Task.CompletedTask);

    // Act
    await _service.AddMessageToCachedChat(userId, chatId, newMessage);

    // Assert
    existingChat.Messages.Should().HaveCount(3);
    existingChat.Messages.Should().Contain(newMessage);
    _mockChatCacheRepo.Verify(x => x.UpdateCachedChatAsync(cacheKey, existingChat, _settings.CacheDuration), Times.Once);
  }

  /// <summary>
  /// Verifies that when chat doesn't exist in cache, nothing happens (no exception).
  /// </summary>
  [Fact]
  public async Task AddMessageToCachedChat_ChatDoesNotExist_DoesNothing()
  {
    // Arrange
    var userId = "user456";
    var chatId = Guid.NewGuid();
    var cacheKey = $"chat:{userId}:{chatId}";
    var newMessage = new ChatMessage { Content = "Test" };

    _mockChatCacheRepo.Setup(x => x.GetCachedChatAsync(cacheKey))
        .ReturnsAsync((CachedChatData?)null);

    // Act
    Func<Task> act = async () => await _service.AddMessageToCachedChat(userId, chatId, newMessage);

    // Assert
    await act.Should().NotThrowAsync();
    _mockChatCacheRepo.Verify(x => x.UpdateCachedChatAsync(It.IsAny<string>(), It.IsAny<CachedChatData>(), It.IsAny<TimeSpan>()), Times.Never);
  }

  /// <summary>
  /// Verifies that message is correctly added to the Messages list.
  /// </summary>
  [Fact]
  public async Task AddMessageToCachedChat_VerifiesMessageIsAdded()
  {
    // Arrange
    var userId = "user123";
    var chatId = Guid.NewGuid();
    var cacheKey = $"chat:{userId}:{chatId}";
    var existingChat = CreateMockCachedChatData(chatId, 1);
    var initialCount = existingChat.Messages!.Count;
    var newMessage = new ChatMessage
    {
      Id = Guid.NewGuid(),
      Content = "Added message",
      Type = MessageType.Assistant
    };

    _mockChatCacheRepo.Setup(x => x.GetCachedChatAsync(cacheKey))
        .ReturnsAsync(existingChat);

    // Act
    await _service.AddMessageToCachedChat(userId, chatId, newMessage);

    // Assert
    existingChat.Messages!.Count.Should().Be(initialCount + 1);
    existingChat.Messages.Last().Should().Be(newMessage);
  }

  #endregion

  #region ChangeCachedChatTitle Tests

  /// <summary>
  /// Verifies that when chat exists, title is updated in metadata.
  /// </summary>
  [Fact]
  public async Task ChangeCachedChatTitle_ChatExists_UpdatesTitleInMetadata()
  {
    // Arrange
    var userId = "user123";
    var chatId = Guid.NewGuid();
    var cacheKey = $"chat:{userId}:{chatId}";
    var newTitle = "Updated Chat Title";
    var existingChat = CreateMockCachedChatData(chatId, 2);
    existingChat.Metadata!.Title = "Old Title";

    _mockChatCacheRepo.Setup(x => x.GetCachedChatAsync(cacheKey))
        .ReturnsAsync(existingChat);

    // Act
    await _service.ChangeCachedChatTitle(userId, chatId, newTitle);

    // Assert
    existingChat.Metadata!.Title.Should().Be(newTitle);
    _mockChatCacheRepo.Verify(x => x.UpdateCachedChatAsync(cacheKey, existingChat, _settings.CacheDuration), Times.Once);
  }

  /// <summary>
  /// Verifies that when chat doesn't exist, nothing happens.
  /// </summary>
  [Fact]
  public async Task ChangeCachedChatTitle_ChatDoesNotExist_DoesNothing()
  {
    // Arrange
    var userId = "user456";
    var chatId = Guid.NewGuid();
    var cacheKey = $"chat:{userId}:{chatId}";
    var newTitle = "New Title";

    _mockChatCacheRepo.Setup(x => x.GetCachedChatAsync(cacheKey))
        .ReturnsAsync((CachedChatData?)null);

    // Act
    Func<Task> act = async () => await _service.ChangeCachedChatTitle(userId, chatId, newTitle);

    // Assert
    await act.Should().NotThrowAsync();
    _mockChatCacheRepo.Verify(x => x.UpdateCachedChatAsync(It.IsAny<string>(), It.IsAny<CachedChatData>(), It.IsAny<TimeSpan>()), Times.Never);
  }

  /// <summary>
  /// Verifies that cache is updated with correct expiration.
  /// </summary>
  [Fact]
  public async Task ChangeCachedChatTitle_VerifiesCacheUpdateWithCorrectExpiration()
  {
    // Arrange
    var userId = "user123";
    var chatId = Guid.NewGuid();
    var cacheKey = $"chat:{userId}:{chatId}";
    var newTitle = "Title Update";
    var existingChat = CreateMockCachedChatData(chatId, 1);

    _mockChatCacheRepo.Setup(x => x.GetCachedChatAsync(cacheKey))
        .ReturnsAsync(existingChat);

    TimeSpan? capturedExpiration = null;
    _mockChatCacheRepo.Setup(x => x.UpdateCachedChatAsync(cacheKey, existingChat, It.IsAny<TimeSpan>()))
        .Callback<string, CachedChatData, TimeSpan>((k, d, exp) => capturedExpiration = exp)
        .Returns(Task.CompletedTask);

    // Act
    await _service.ChangeCachedChatTitle(userId, chatId, newTitle);

    // Assert
    capturedExpiration.Should().NotBeNull();
    capturedExpiration!.Value.TotalHours.Should().Be(2);
  }

  #endregion

  #region ChangeCachedChatContextCountStatus Tests

  /// <summary>
  /// Verifies that when chat exists, IsContextFull is set to true.
  /// </summary>
  [Fact]
  public async Task ChangeCachedChatContextCountStatus_ChatExists_SetsIsContextFullToTrue()
  {
    // Arrange
    var userId = "user123";
    var chatId = Guid.NewGuid();
    var cacheKey = $"chat:{userId}:{chatId}";
    var existingChat = CreateMockCachedChatData(chatId, 3);
    existingChat.Metadata!.IsContextFull = false;

    _mockChatCacheRepo.Setup(x => x.GetCachedChatAsync(cacheKey))
        .ReturnsAsync(existingChat);

    // Act
    await _service.ChangeCachedChatContextCountStatus(userId, chatId);

    // Assert
    existingChat.Metadata!.IsContextFull.Should().BeTrue();
    _mockChatCacheRepo.Verify(x => x.UpdateCachedChatAsync(cacheKey, existingChat, _settings.CacheDuration), Times.Once);
  }

  /// <summary>
  /// Verifies that when chat doesn't exist, nothing happens.
  /// </summary>
  [Fact]
  public async Task ChangeCachedChatContextCountStatus_ChatDoesNotExist_DoesNothing()
  {
    // Arrange
    var userId = "user456";
    var chatId = Guid.NewGuid();
    var cacheKey = $"chat:{userId}:{chatId}";

    _mockChatCacheRepo.Setup(x => x.GetCachedChatAsync(cacheKey))
        .ReturnsAsync((CachedChatData?)null);

    // Act
    Func<Task> act = async () => await _service.ChangeCachedChatContextCountStatus(userId, chatId);

    // Assert
    await act.Should().NotThrowAsync();
    _mockChatCacheRepo.Verify(x => x.UpdateCachedChatAsync(It.IsAny<string>(), It.IsAny<CachedChatData>(), It.IsAny<TimeSpan>()), Times.Never);
  }

  /// <summary>
  /// Verifies that metadata is correctly updated.
  /// </summary>
  [Fact]
  public async Task ChangeCachedChatContextCountStatus_VerifiesMetadataUpdate()
  {
    // Arrange
    var userId = "user123";
    var chatId = Guid.NewGuid();
    var cacheKey = $"chat:{userId}:{chatId}";
    var existingChat = CreateMockCachedChatData(chatId, 2);
    existingChat.Metadata!.IsContextFull = false;

    _mockChatCacheRepo.Setup(x => x.GetCachedChatAsync(cacheKey))
        .ReturnsAsync(existingChat);

    var wasUpdated = false;
    _mockChatCacheRepo.Setup(x => x.UpdateCachedChatAsync(cacheKey, existingChat, It.IsAny<TimeSpan>()))
        .Callback(() => wasUpdated = true)
        .Returns(Task.CompletedTask);

    // Act
    await _service.ChangeCachedChatContextCountStatus(userId, chatId);

    // Assert
    existingChat.Metadata!.IsContextFull.Should().BeTrue();
    wasUpdated.Should().BeTrue();
  }

  #endregion

  #region DeleteCachedChat Tests

  /// <summary>
  /// Verifies that DeleteCachedChat calls repository with correct cache key.
  /// </summary>
  [Fact]
  public async Task DeleteCachedChat_CallsRepositoryWithCorrectKey()
  {
    // Arrange
    var userId = "user123";
    var chatId = Guid.NewGuid();
    var expectedKey = $"chat:{userId}:{chatId}";

    _mockChatCacheRepo.Setup(x => x.DeleteCachedChatAsync(expectedKey))
        .Returns(Task.CompletedTask);

    // Act
    await _service.DeleteCachedChat(userId, chatId);

    // Assert
    _mockChatCacheRepo.Verify(x => x.DeleteCachedChatAsync(expectedKey), Times.Once);
  }

  #endregion

  #region GetStarredMessagesFromCache Tests

  /// <summary>
  /// Verifies that starred messages are returned for a user.
  /// </summary>
  [Fact]
  public async Task GetStarredMessagesFromCache_ReturnsStarredMessages()
  {
    // Arrange
    var userId = "user123";
    var expectedPattern = $"chat:{userId}:*";
    var starredMessages = new List<ChatMessage>
    {
      new ChatMessage { Id = Guid.NewGuid(), Content = "Starred 1", IsStarred = true },
      new ChatMessage { Id = Guid.NewGuid(), Content = "Starred 2", IsStarred = true }
    };

    _mockChatCacheRepo.Setup(x => x.GetAllStarredMessagesAsync(expectedPattern))
        .ReturnsAsync(starredMessages);

    // Act
    var result = await _service.GetStarredMessagesFromCache(userId);

    // Assert
    result.Should().NotBeNull();
    result.Should().HaveCount(2);
    result.Should().BeEquivalentTo(starredMessages);
  }

  /// <summary>
  /// Verifies that correct key pattern is used for user.
  /// </summary>
  [Fact]
  public async Task GetStarredMessagesFromCache_UsesCorrectKeyPattern()
  {
    // Arrange
    var userId = "user456";
    var expectedPattern = $"chat:{userId}:*";

    _mockChatCacheRepo.Setup(x => x.GetAllStarredMessagesAsync(expectedPattern))
        .ReturnsAsync(new List<ChatMessage>());

    // Act
    await _service.GetStarredMessagesFromCache(userId);

    // Assert
    _mockChatCacheRepo.Verify(x => x.GetAllStarredMessagesAsync(expectedPattern), Times.Once);
  }

  #endregion

  #region ToggleStarredMessageInCache Tests

  /// <summary>
  /// Verifies that when message exists and IsStarred is false, it's set to true.
  /// </summary>
  [Fact]
  public async Task ToggleStarredMessageInCache_MessageExistsNotStarred_SetsToTrue()
  {
    // Arrange
    var userId = "user123";
    var chatId = Guid.NewGuid();
    var messageId = Guid.NewGuid();
    var cacheKey = $"chat:{userId}:{chatId}";
    var existingChat = CreateMockCachedChatData(chatId, 3);
    var messageToToggle = existingChat.Messages!.First();
    messageToToggle.Id = messageId;
    messageToToggle.IsStarred = false;

    _mockChatCacheRepo.Setup(x => x.GetCachedChatAsync(cacheKey))
        .ReturnsAsync(existingChat);

    // Act
    await _service.ToggleStarredMessageInCache(userId, chatId, messageId);

    // Assert
    messageToToggle.IsStarred.Should().BeTrue();
    _mockChatCacheRepo.Verify(x => x.UpdateCachedChatAsync(cacheKey, existingChat, _settings.CacheDuration), Times.Once);
  }

  /// <summary>
  /// Verifies that when message exists and IsStarred is true, it's set to false.
  /// </summary>
  [Fact]
  public async Task ToggleStarredMessageInCache_MessageExistsStarred_SetsToFalse()
  {
    // Arrange
    var userId = "user123";
    var chatId = Guid.NewGuid();
    var messageId = Guid.NewGuid();
    var cacheKey = $"chat:{userId}:{chatId}";
    var existingChat = CreateMockCachedChatData(chatId, 2);
    var messageToToggle = existingChat.Messages!.First();
    messageToToggle.Id = messageId;
    messageToToggle.IsStarred = true;

    _mockChatCacheRepo.Setup(x => x.GetCachedChatAsync(cacheKey))
        .ReturnsAsync(existingChat);

    // Act
    await _service.ToggleStarredMessageInCache(userId, chatId, messageId);

    // Assert
    messageToToggle.IsStarred.Should().BeFalse();
    _mockChatCacheRepo.Verify(x => x.UpdateCachedChatAsync(cacheKey, existingChat, _settings.CacheDuration), Times.Once);
  }

  /// <summary>
  /// Verifies that when chat doesn't exist, nothing happens.
  /// </summary>
  [Fact]
  public async Task ToggleStarredMessageInCache_ChatDoesNotExist_DoesNothing()
  {
    // Arrange
    var userId = "user456";
    var chatId = Guid.NewGuid();
    var messageId = Guid.NewGuid();
    var cacheKey = $"chat:{userId}:{chatId}";

    _mockChatCacheRepo.Setup(x => x.GetCachedChatAsync(cacheKey))
        .ReturnsAsync((CachedChatData?)null);

    // Act
    Func<Task> act = async () => await _service.ToggleStarredMessageInCache(userId, chatId, messageId);

    // Assert
    await act.Should().NotThrowAsync();
    _mockChatCacheRepo.Verify(x => x.UpdateCachedChatAsync(It.IsAny<string>(), It.IsAny<CachedChatData>(), It.IsAny<TimeSpan>()), Times.Never);
  }

  /// <summary>
  /// Verifies that when message doesn't exist in chat, nothing happens.
  /// </summary>
  [Fact]
  public async Task ToggleStarredMessageInCache_MessageDoesNotExist_DoesNothing()
  {
    // Arrange
    var userId = "user123";
    var chatId = Guid.NewGuid();
    var messageId = Guid.NewGuid(); // Non-existent message
    var cacheKey = $"chat:{userId}:{chatId}";
    var existingChat = CreateMockCachedChatData(chatId, 2);

    _mockChatCacheRepo.Setup(x => x.GetCachedChatAsync(cacheKey))
        .ReturnsAsync(existingChat);

    // Act
    await _service.ToggleStarredMessageInCache(userId, chatId, messageId);

    // Assert - Update should still be called, but no message should have changed
    var anyMessageStarred = existingChat.Messages!.Any(m => m.IsStarred);
    anyMessageStarred.Should().BeFalse();
    _mockChatCacheRepo.Verify(x => x.UpdateCachedChatAsync(cacheKey, existingChat, _settings.CacheDuration), Times.Once);
  }

  #endregion

  #region SetReportedMessage Tests

  /// <summary>
  /// Verifies that when message exists, IsReported is set to true.
  /// </summary>
  [Fact]
  public async Task SetReportedMessage_MessageExists_SetsIsReportedToTrue()
  {
    // Arrange
    var userId = "user123";
    var chatId = Guid.NewGuid();
    var messageId = Guid.NewGuid();
    var cacheKey = $"chat:{userId}:{chatId}";
    var existingChat = CreateMockCachedChatData(chatId, 3);
    var messageToReport = existingChat.Messages!.First();
    messageToReport.Id = messageId;
    messageToReport.IsReported = false;

    var reportedMessage = new ChatMessage
    {
      Id = messageId,
      ChatId = chatId,
      Content = messageToReport.Content
    };

    _mockChatCacheRepo.Setup(x => x.GetCachedChatAsync(cacheKey))
        .ReturnsAsync(existingChat);

    // Act
    await _service.SetReportedMessage(userId, reportedMessage);

    // Assert
    messageToReport.IsReported.Should().BeTrue();
    _mockChatCacheRepo.Verify(x => x.UpdateCachedChatAsync(cacheKey, existingChat, _settings.CacheDuration), Times.Once);
  }

  /// <summary>
  /// Verifies that when chat doesn't exist, nothing happens.
  /// </summary>
  [Fact]
  public async Task SetReportedMessage_ChatDoesNotExist_DoesNothing()
  {
    // Arrange
    var userId = "user456";
    var chatId = Guid.NewGuid();
    var messageId = Guid.NewGuid();
    var cacheKey = $"chat:{userId}:{chatId}";

    var reportedMessage = new ChatMessage
    {
      Id = messageId,
      ChatId = chatId,
      Content = "Test"
    };

    _mockChatCacheRepo.Setup(x => x.GetCachedChatAsync(cacheKey))
        .ReturnsAsync((CachedChatData?)null);

    // Act
    Func<Task> act = async () => await _service.SetReportedMessage(userId, reportedMessage);

    // Assert
    await act.Should().NotThrowAsync();
    _mockChatCacheRepo.Verify(x => x.UpdateCachedChatAsync(It.IsAny<string>(), It.IsAny<CachedChatData>(), It.IsAny<TimeSpan>()), Times.Never);
  }

  /// <summary>
  /// Verifies that when message doesn't exist in chat, nothing happens.
  /// </summary>
  [Fact]
  public async Task SetReportedMessage_MessageDoesNotExist_DoesNothing()
  {
    // Arrange
    var userId = "user123";
    var chatId = Guid.NewGuid();
    var messageId = Guid.NewGuid(); // Non-existent message
    var cacheKey = $"chat:{userId}:{chatId}";
    var existingChat = CreateMockCachedChatData(chatId, 2);

    var reportedMessage = new ChatMessage
    {
      Id = messageId,
      ChatId = chatId,
      Content = "Non-existent"
    };

    _mockChatCacheRepo.Setup(x => x.GetCachedChatAsync(cacheKey))
        .ReturnsAsync(existingChat);

    // Act
    await _service.SetReportedMessage(userId, reportedMessage);

    // Assert - Update should NOT be called since message doesn't exist
    var anyMessageReported = existingChat.Messages!.Any(m => m.IsReported);
    anyMessageReported.Should().BeFalse();
    _mockChatCacheRepo.Verify(x => x.UpdateCachedChatAsync(It.IsAny<string>(), It.IsAny<CachedChatData>(), It.IsAny<TimeSpan>()), Times.Never);
  }

  /// <summary>
  /// Verifies that cache update is called when reporting message.
  /// </summary>
  [Fact]
  public async Task SetReportedMessage_VerifiesCacheUpdateIsCalled()
  {
    // Arrange
    var userId = "user123";
    var chatId = Guid.NewGuid();
    var messageId = Guid.NewGuid();
    var cacheKey = $"chat:{userId}:{chatId}";
    var existingChat = CreateMockCachedChatData(chatId, 1);
    var messageToReport = existingChat.Messages!.First();
    messageToReport.Id = messageId;

    var reportedMessage = new ChatMessage
    {
      Id = messageId,
      ChatId = chatId
    };

    _mockChatCacheRepo.Setup(x => x.GetCachedChatAsync(cacheKey))
        .ReturnsAsync(existingChat);

    var updateCalled = false;
    _mockChatCacheRepo.Setup(x => x.UpdateCachedChatAsync(cacheKey, existingChat, _settings.CacheDuration))
        .Callback(() => updateCalled = true)
        .Returns(Task.CompletedTask);

    // Act
    await _service.SetReportedMessage(userId, reportedMessage);

    // Assert
    updateCalled.Should().BeTrue();
  }

  #endregion

  #region GenerateCacheKey Tests

  /// <summary>
  /// Verifies that cache key is generated in correct format.
  /// </summary>
  [Fact]
  public async Task GenerateCacheKey_GeneratesCorrectFormat()
  {
    // Arrange
    var userId = "testuser";
    var chatId = Guid.Parse("12345678-1234-1234-1234-123456789012");
    var expectedKey = $"chat:{userId}:{chatId}";

    string? capturedKey = null;
    _mockChatCacheRepo.Setup(x => x.GetCachedChatAsync(It.IsAny<string>()))
        .Callback<string>(key => capturedKey = key)
        .ReturnsAsync((CachedChatData?)null);

    // Act
    await _service.GetCachedChat(userId, chatId);

    // Assert
    capturedKey.Should().Be(expectedKey);
  }

  /// <summary>
  /// Verifies that different users and chats generate different keys.
  /// </summary>
  [Fact]
  public async Task GenerateCacheKey_DifferentUsersAndChats_GeneratesDifferentKeys()
  {
    // Arrange
    var userId1 = "user1";
    var userId2 = "user2";
    var chatId1 = Guid.NewGuid();
    var chatId2 = Guid.NewGuid();

    var capturedKeys = new List<string>();
    _mockChatCacheRepo.Setup(x => x.GetCachedChatAsync(It.IsAny<string>()))
        .Callback<string>(key => capturedKeys.Add(key))
        .ReturnsAsync((CachedChatData?)null);

    // Act
    await _service.GetCachedChat(userId1, chatId1);
    await _service.GetCachedChat(userId1, chatId2);
    await _service.GetCachedChat(userId2, chatId1);

    // Assert
    capturedKeys.Should().HaveCount(3);
    capturedKeys.Distinct().Should().HaveCount(3); // All keys should be unique
  }

  #endregion

  #region Helper Methods

  private CachedChatData CreateMockCachedChatData(Guid chatId, int messageCount)
  {
    var messages = new List<ChatMessage>();
    for (int i = 0; i < messageCount; i++)
    {
      messages.Add(new ChatMessage
      {
        Id = Guid.NewGuid(),
        ChatId = chatId,
        Content = $"Message {i + 1}",
        Type = i % 2 == 0 ? MessageType.User : MessageType.Assistant,
        CreatedAt = DateTime.UtcNow.AddMinutes(-i),
        IsStarred = false,
        IsReported = false
      });
    }

    return new CachedChatData
    {
      Metadata = new ChatMetaDataDto
      {
        Id = chatId,
        Title = "Test Chat",
        IsContextFull = false,
        CreatedAt = DateTime.UtcNow
      },
      Messages = messages
    };
  }

  #endregion
}
