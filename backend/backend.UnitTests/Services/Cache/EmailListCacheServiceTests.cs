using Xunit;
using Moq;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using backend.Services.Classes;
using backend.Repository.Interfaces;
using backend.Models.Dtos;

namespace backend.UnitTests.Services.Cache;

public class EmailListCacheServiceTests
{
  private readonly Mock<IRedisCacheRepo> _mockRedisCacheRepo;
  private readonly Mock<ILogger<EmailListCacheService>> _mockLogger;
  private readonly EmailListCacheService _emailListCacheService;
  private const string CacheKey = "newsletter:subscribers";

  public EmailListCacheServiceTests()
  {
    _mockRedisCacheRepo = new Mock<IRedisCacheRepo>();
    _mockLogger = new Mock<ILogger<EmailListCacheService>>();

    _emailListCacheService = new EmailListCacheService(
        _mockRedisCacheRepo.Object,
        _mockLogger.Object
    );
  }

  #region ToggleUserFromNewsletterAsync Tests

  /// <summary>
  /// Verifies that when user is subscribed, they are removed and then re-added to the newsletter list.
  /// </summary>
  [Fact]
  public async Task ToggleUserFromNewsletterAsync_WhenUserIsSubscribed_RemovesAndAddsUser()
  {
    // Arrange
    var user = new EmailNewsletterDTO
    {
      Email = "user@example.com",
      FullName = "John Doe"
    };

    _mockRedisCacheRepo
        .Setup(x => x.IsUserInNewsletterListAsync(CacheKey, user.Email))
        .ReturnsAsync(true);

    _mockRedisCacheRepo
        .Setup(x => x.RemoveUserFromNewsletterListAsync(CacheKey, user.Email))
        .Returns(Task.CompletedTask);

    _mockRedisCacheRepo
        .Setup(x => x.AddUserToNewsletterListAsync(CacheKey, user))
        .Returns(Task.CompletedTask);

    // Act
    await _emailListCacheService.ToggleUserFromNewsletterAsync(user);

    // Assert
    _mockRedisCacheRepo.Verify(x => x.IsUserInNewsletterListAsync(CacheKey, user.Email), Times.Once);
    _mockRedisCacheRepo.Verify(x => x.RemoveUserFromNewsletterListAsync(CacheKey, user.Email), Times.Once);
    _mockRedisCacheRepo.Verify(x => x.AddUserToNewsletterListAsync(CacheKey, user), Times.Once);
  }

  /// <summary>
  /// Verifies that when user is not subscribed, they are only added to the newsletter list.
  /// </summary>
  [Fact]
  public async Task ToggleUserFromNewsletterAsync_WhenUserIsNotSubscribed_OnlyAddsUser()
  {
    // Arrange
    var user = new EmailNewsletterDTO
    {
      Email = "newuser@example.com",
      FullName = "Jane Doe"
    };

    _mockRedisCacheRepo
        .Setup(x => x.IsUserInNewsletterListAsync(CacheKey, user.Email))
        .ReturnsAsync(false);

    _mockRedisCacheRepo
        .Setup(x => x.AddUserToNewsletterListAsync(CacheKey, user))
        .Returns(Task.CompletedTask);

    // Act
    await _emailListCacheService.ToggleUserFromNewsletterAsync(user);

    // Assert
    _mockRedisCacheRepo.Verify(x => x.IsUserInNewsletterListAsync(CacheKey, user.Email), Times.Once);
    _mockRedisCacheRepo.Verify(x => x.RemoveUserFromNewsletterListAsync(CacheKey, user.Email), Times.Never);
    _mockRedisCacheRepo.Verify(x => x.AddUserToNewsletterListAsync(CacheKey, user), Times.Once);
  }

  /// <summary>
  /// Verifies that when an exception occurs during toggle, it is logged and rethrown.
  /// </summary>
  [Fact]
  public async Task ToggleUserFromNewsletterAsync_WhenExceptionThrown_LogsErrorAndRethrows()
  {
    // Arrange
    var user = new EmailNewsletterDTO
    {
      Email = "user@example.com",
      FullName = "John Doe"
    };
    var exception = new Exception("Redis error");

    _mockRedisCacheRepo
        .Setup(x => x.IsUserInNewsletterListAsync(CacheKey, user.Email))
        .ThrowsAsync(exception);

    // Act & Assert
    await Assert.ThrowsAsync<Exception>(
        async () => await _emailListCacheService.ToggleUserFromNewsletterAsync(user));

    _mockLogger.Verify(
        x => x.Log(
            LogLevel.Error,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Error adding user to newsletter")),
            exception,
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
        Times.Once);
  }

  #endregion

  #region RemoveUserFromNewsletterAsync Tests

  /// <summary>
  /// Verifies that removing a user from the newsletter calls the repository method.
  /// </summary>
  [Fact]
  public async Task RemoveUserFromNewsletterAsync_WithValidEmail_RemovesUserFromCache()
  {
    // Arrange
    var email = "user@example.com";

    _mockRedisCacheRepo
        .Setup(x => x.RemoveUserFromNewsletterListAsync(CacheKey, email))
        .Returns(Task.CompletedTask);

    // Act
    await _emailListCacheService.RemoveUserFromNewsletterAsync(email);

    // Assert
    _mockRedisCacheRepo.Verify(x => x.RemoveUserFromNewsletterListAsync(CacheKey, email), Times.Once);
    _mockLogger.Verify(
        x => x.Log(
            LogLevel.Information,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains($"Removed user {email} from newsletter subscribers")),
            null,
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
        Times.Once);
  }

  /// <summary>
  /// Verifies that when removal throws an exception, it is logged and rethrown.
  /// </summary>
  [Fact]
  public async Task RemoveUserFromNewsletterAsync_WhenExceptionThrown_LogsErrorAndRethrows()
  {
    // Arrange
    var email = "user@example.com";
    var exception = new Exception("Redis error");

    _mockRedisCacheRepo
        .Setup(x => x.RemoveUserFromNewsletterListAsync(CacheKey, email))
        .ThrowsAsync(exception);

    // Act & Assert
    await Assert.ThrowsAsync<Exception>(
        async () => await _emailListCacheService.RemoveUserFromNewsletterAsync(email));

    _mockLogger.Verify(
        x => x.Log(
            LogLevel.Error,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Error removing user from newsletter")),
            exception,
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
        Times.Once);
  }

  #endregion

  #region GetNewsletterRecipients Tests

  /// <summary>
  /// Verifies that getting newsletter recipients returns the list from the repository.
  /// </summary>
  [Fact]
  public async Task GetNewsletterRecipients_WithSubscribers_ReturnsSubscriberList()
  {
    // Arrange
    var subscribers = new List<EmailNewsletterDTO>
    {
      new EmailNewsletterDTO { Email = "user1@example.com", FullName = "User One" },
      new EmailNewsletterDTO { Email = "user2@example.com", FullName = "User Two" },
      new EmailNewsletterDTO { Email = "user3@example.com", FullName = "User Three" }
    };

    _mockRedisCacheRepo
        .Setup(x => x.GetNewsletterSubscribersListAsync(CacheKey))
        .ReturnsAsync(subscribers);

    // Act
    var result = await _emailListCacheService.GetNewsletterRecipients();

    // Assert
    result.Should().NotBeNull();
    result.Should().HaveCount(3);
    result.Should().BeEquivalentTo(subscribers);
    _mockRedisCacheRepo.Verify(x => x.GetNewsletterSubscribersListAsync(CacheKey), Times.Once);
  }

  /// <summary>
  /// Verifies that getting newsletter recipients with no subscribers returns an empty list.
  /// </summary>
  [Fact]
  public async Task GetNewsletterRecipients_WithNoSubscribers_ReturnsEmptyList()
  {
    // Arrange
    var emptyList = new List<EmailNewsletterDTO>();

    _mockRedisCacheRepo
        .Setup(x => x.GetNewsletterSubscribersListAsync(CacheKey))
        .ReturnsAsync(emptyList);

    // Act
    var result = await _emailListCacheService.GetNewsletterRecipients();

    // Assert
    result.Should().NotBeNull();
    result.Should().BeEmpty();
    _mockRedisCacheRepo.Verify(x => x.GetNewsletterSubscribersListAsync(CacheKey), Times.Once);
  }

  /// <summary>
  /// Verifies that when getting recipients throws an exception, an empty list is returned and error is logged.
  /// </summary>
  [Fact]
  public async Task GetNewsletterRecipients_WhenExceptionThrown_ReturnsEmptyListAndLogsError()
  {
    // Arrange
    var exception = new Exception("Redis error");

    _mockRedisCacheRepo
        .Setup(x => x.GetNewsletterSubscribersListAsync(CacheKey))
        .ThrowsAsync(exception);

    // Act
    var result = await _emailListCacheService.GetNewsletterRecipients();

    // Assert
    result.Should().NotBeNull();
    result.Should().BeEmpty();
    _mockLogger.Verify(
        x => x.Log(
            LogLevel.Error,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Error getting newsletter list")),
            exception,
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
        Times.Once);
  }

  #endregion

  #region UpdateUserInfo Tests

  /// <summary>
  /// Verifies that updating user info removes the old entry and adds a new one with updated information.
  /// </summary>
  [Fact]
  public async Task UpdateUserInfo_WithValidData_RemovesOldAndAddsNewEntry()
  {
    // Arrange
    var email = "user@example.com";
    var fullName = "Updated Name";

    _mockRedisCacheRepo
        .Setup(x => x.RemoveUserFromNewsletterListAsync(CacheKey, email))
        .Returns(Task.CompletedTask);

    _mockRedisCacheRepo
        .Setup(x => x.IsUserInNewsletterListAsync(CacheKey, email))
        .ReturnsAsync(false);

    _mockRedisCacheRepo
        .Setup(x => x.AddUserToNewsletterListAsync(CacheKey, It.Is<EmailNewsletterDTO>(
            dto => dto.Email == email && dto.FullName == fullName)))
        .Returns(Task.CompletedTask);

    // Act
    await _emailListCacheService.UpdateUserInfo(email, fullName);

    // Assert
    _mockRedisCacheRepo.Verify(x => x.RemoveUserFromNewsletterListAsync(CacheKey, email), Times.Once);
    _mockRedisCacheRepo.Verify(
        x => x.AddUserToNewsletterListAsync(CacheKey, It.Is<EmailNewsletterDTO>(
            dto => dto.Email == email && dto.FullName == fullName)),
        Times.Once);
  }

  #endregion
}
