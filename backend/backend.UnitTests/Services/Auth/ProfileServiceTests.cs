using Xunit;
using Moq;
using FluentAssertions;
using AutoMapper;
using backend.Services.Classes;
using backend.Repository.Interfaces;
using backend.Services.Interfaces;
using backend.Services.Interfaces.Auth;
using backend.Models.Dtos;
using backend.Models.Domain;
using Microsoft.AspNetCore.Identity;

namespace backend.UnitTests.Services.Auth;

public class ProfileServiceTests
{
  private readonly Mock<IProfileRepo> _mockProfileRepo;
  private readonly Mock<ITokenService> _mockTokenService;
  private readonly Mock<IAuthRepo> _mockAuthRepo;
  private readonly Mock<ICookieService> _mockCookieService;
  private readonly Mock<IEmailListCacheService> _mockEmailListCacheService;
  private readonly Mock<IMapper> _mockMapper;
  private readonly ProfileService _profileService;

  public ProfileServiceTests()
  {
    _mockProfileRepo = new Mock<IProfileRepo>();
    _mockTokenService = new Mock<ITokenService>();
    _mockAuthRepo = new Mock<IAuthRepo>();
    _mockCookieService = new Mock<ICookieService>();
    _mockEmailListCacheService = new Mock<IEmailListCacheService>();
    _mockMapper = new Mock<IMapper>();

    _profileService = new ProfileService(
        _mockProfileRepo.Object,
        _mockTokenService.Object,
        _mockAuthRepo.Object,
        _mockCookieService.Object,
        _mockEmailListCacheService.Object,
        _mockMapper.Object
    );
  }

  #region ChangeEmailAsync Tests

  /// <summary>
  /// Verifies that changing email successfully updates the email in the repository and cache.
  /// </summary>
  [Fact]
  public async Task ChangeEmailAsync_WithValidEmail_UpdatesEmailAndCache()
  {
    // Arrange
    var userId = "user123";
    var newEmail = "newemail@example.com";
    var fullName = "John Doe";
    var successResult = IdentityResult.Success;

    _mockProfileRepo
        .Setup(x => x.ChangeProfileEmailAsync(userId, newEmail))
        .ReturnsAsync((fullName, successResult));

    _mockEmailListCacheService
        .Setup(x => x.UpdateUserInfo(newEmail, fullName))
        .Returns(Task.CompletedTask);

    // Act
    var result = await _profileService.ChangeEmailAsync(newEmail, userId);

    // Assert
    result.Should().Be(successResult);
    result.Succeeded.Should().BeTrue();
    _mockProfileRepo.Verify(x => x.ChangeProfileEmailAsync(userId, newEmail), Times.Once);
    _mockEmailListCacheService.Verify(x => x.UpdateUserInfo(newEmail, fullName), Times.Once);
  }

  /// <summary>
  /// Verifies that when email change fails, the cache is not updated.
  /// </summary>
  [Fact]
  public async Task ChangeEmailAsync_WhenChangeFails_DoesNotUpdateCache()
  {
    // Arrange
    var userId = "user123";
    var newEmail = "newemail@example.com";
    var failureResult = IdentityResult.Failed(new IdentityError { Description = "Email already exists" });

    _mockProfileRepo
        .Setup(x => x.ChangeProfileEmailAsync(userId, newEmail))
        .ReturnsAsync(("", failureResult));

    // Act
    var result = await _profileService.ChangeEmailAsync(newEmail, userId);

    // Assert
    result.Should().Be(failureResult);
    result.Succeeded.Should().BeFalse();
    _mockEmailListCacheService.Verify(x => x.UpdateUserInfo(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
  }

  #endregion

  #region ChangeFullNameAsync Tests

  /// <summary>
  /// Verifies that changing full name successfully updates the name in the repository and cache.
  /// </summary>
  [Fact]
  public async Task ChangeFullNameAsync_WithValidName_UpdatesNameAndCache()
  {
    // Arrange
    var userId = "user123";
    var newName = "Jane Doe";
    var email = "user@example.com";
    var successResult = IdentityResult.Success;

    _mockProfileRepo
        .Setup(x => x.ChangeProfileFullNameAsync(userId, newName))
        .ReturnsAsync((email, successResult));

    _mockEmailListCacheService
        .Setup(x => x.UpdateUserInfo(email, newName))
        .Returns(Task.CompletedTask);

    // Act
    var result = await _profileService.ChangeFullNameAsync(newName, userId);

    // Assert
    result.Should().Be(successResult);
    result.Succeeded.Should().BeTrue();
    _mockProfileRepo.Verify(x => x.ChangeProfileFullNameAsync(userId, newName), Times.Once);
    _mockEmailListCacheService.Verify(x => x.UpdateUserInfo(email, newName), Times.Once);
  }

  /// <summary>
  /// Verifies that when full name change fails, the cache is not updated.
  /// </summary>
  [Fact]
  public async Task ChangeFullNameAsync_WhenChangeFails_DoesNotUpdateCache()
  {
    // Arrange
    var userId = "user123";
    var newName = "Jane Doe";
    var failureResult = IdentityResult.Failed(new IdentityError { Description = "User not found" });

    _mockProfileRepo
        .Setup(x => x.ChangeProfileFullNameAsync(userId, newName))
        .ReturnsAsync(("", failureResult));

    // Act
    var result = await _profileService.ChangeFullNameAsync(newName, userId);

    // Assert
    result.Should().Be(failureResult);
    result.Succeeded.Should().BeFalse();
    _mockEmailListCacheService.Verify(x => x.UpdateUserInfo(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
  }

  #endregion

  #region ChangePasswordAsync Tests

  /// <summary>
  /// Verifies that changing password with valid credentials succeeds.
  /// </summary>
  [Fact]
  public async Task ChangePasswordAsync_WithValidCredentials_ReturnsSuccess()
  {
    // Arrange
    var userId = "user123";
    var newPassword = "NewPassword123!";
    var currentPassword = "OldPassword123!";
    var successResult = IdentityResult.Success;

    _mockProfileRepo
        .Setup(x => x.ChangeProfilePasswordAsync(userId, newPassword, currentPassword))
        .ReturnsAsync(successResult);

    // Act
    var result = await _profileService.ChangePasswordAsync(newPassword, currentPassword, userId);

    // Assert
    result.Should().Be(successResult);
    result.Succeeded.Should().BeTrue();
    _mockProfileRepo.Verify(x => x.ChangeProfilePasswordAsync(userId, newPassword, currentPassword), Times.Once);
  }

  /// <summary>
  /// Verifies that changing password with invalid current password fails.
  /// </summary>
  [Fact]
  public async Task ChangePasswordAsync_WithInvalidCurrentPassword_ReturnsFailure()
  {
    // Arrange
    var userId = "user123";
    var newPassword = "NewPassword123!";
    var currentPassword = "WrongPassword";
    var failureResult = IdentityResult.Failed(new IdentityError { Description = "Incorrect password" });

    _mockProfileRepo
        .Setup(x => x.ChangeProfilePasswordAsync(userId, newPassword, currentPassword))
        .ReturnsAsync(failureResult);

    // Act
    var result = await _profileService.ChangePasswordAsync(newPassword, currentPassword, userId);

    // Assert
    result.Should().Be(failureResult);
    result.Succeeded.Should().BeFalse();
  }

  #endregion

  #region DeleteUserAsync Tests

  /// <summary>
  /// Verifies that deleting a user successfully removes them from the repository and cache.
  /// </summary>
  [Fact]
  public async Task DeleteUserAsync_WithValidUser_DeletesUserAndRemovesFromCache()
  {
    // Arrange
    var userId = "user123";
    var email = "user@example.com";
    var successResult = IdentityResult.Success;

    _mockProfileRepo
        .Setup(x => x.DeleteProfileAsync(userId))
        .ReturnsAsync((email, successResult));

    _mockEmailListCacheService
        .Setup(x => x.RemoveUserFromNewsletterAsync(email))
        .Returns(Task.CompletedTask);

    // Act
    var result = await _profileService.DeleteUserAsync(userId);

    // Assert
    result.Should().Be(successResult);
    result.Succeeded.Should().BeTrue();
    _mockProfileRepo.Verify(x => x.DeleteProfileAsync(userId), Times.Once);
    _mockEmailListCacheService.Verify(x => x.RemoveUserFromNewsletterAsync(email), Times.Once);
  }

  /// <summary>
  /// Verifies that when user deletion fails, the cache is not updated.
  /// </summary>
  [Fact]
  public async Task DeleteUserAsync_WhenDeletionFails_DoesNotRemoveFromCache()
  {
    // Arrange
    var userId = "user123";
    var failureResult = IdentityResult.Failed(new IdentityError { Description = "User not found" });

    _mockProfileRepo
        .Setup(x => x.DeleteProfileAsync(userId))
        .ReturnsAsync(("", failureResult));

    // Act
    var result = await _profileService.DeleteUserAsync(userId);

    // Assert
    result.Should().Be(failureResult);
    result.Succeeded.Should().BeFalse();
    _mockEmailListCacheService.Verify(x => x.RemoveUserFromNewsletterAsync(It.IsAny<string>()), Times.Never);
  }

  #endregion

  #region UpdateJWT Tests

  /// <summary>
  /// Verifies that updating JWT for a valid user with roles generates a new token and sets a cookie.
  /// </summary>
  [Fact]
  public async Task UpdateJWT_WithValidUserAndRoles_GeneratesTokenAndSetsCookie()
  {
    // Arrange
    var userId = "user123";
    var user = new ApiUser
    {
      Id = userId,
      Email = "user@example.com",
      FullName = "John Doe"
    };
    var roles = new List<string> { "free", "user" };
    var token = "jwt-token-string";
    var apiUserDto = new ApiUserDto
    {
      Email = user.Email,
      FullName = user.FullName
    };

    _mockProfileRepo
        .Setup(x => x.GetUserById(userId))
        .ReturnsAsync(user);

    _mockAuthRepo
        .Setup(x => x.GetRolesAsync(user))
        .ReturnsAsync(roles);

    _mockTokenService
        .Setup(x => x.GenerateToken(user, roles))
        .Returns(token);

    _mockMapper
        .Setup(x => x.Map<ApiUserDto>(user))
        .Returns(apiUserDto);

    _mockCookieService
        .Setup(x => x.SetAuthCookie(token));

    // Act
    var result = await _profileService.UpdateJWT(userId);

    // Assert
    result.Should().NotBeNull();
    result!.Roles.Should().BeEquivalentTo(roles);
    result.ApiUserDto.Should().BeEquivalentTo(apiUserDto);
    _mockTokenService.Verify(x => x.GenerateToken(user, roles), Times.Once);
    _mockCookieService.Verify(x => x.SetAuthCookie(token), Times.Once);
  }

  /// <summary>
  /// Verifies that updating JWT for a non-existent user returns null.
  /// </summary>
  [Fact]
  public async Task UpdateJWT_WithNonExistentUser_ReturnsNull()
  {
    // Arrange
    var userId = "nonexistent";

    _mockProfileRepo
        .Setup(x => x.GetUserById(userId))
        .ReturnsAsync((ApiUser?)null);

    // Act
    var result = await _profileService.UpdateJWT(userId);

    // Assert
    result.Should().BeNull();
    _mockAuthRepo.Verify(x => x.GetRolesAsync(It.IsAny<ApiUser>()), Times.Never);
    _mockTokenService.Verify(x => x.GenerateToken(It.IsAny<ApiUser>(), It.IsAny<List<string>>()), Times.Never);
  }

  /// <summary>
  /// Verifies that updating JWT for a user with no roles returns null.
  /// </summary>
  [Fact]
  public async Task UpdateJWT_WithUserWithoutRoles_ReturnsNull()
  {
    // Arrange
    var userId = "user123";
    var user = new ApiUser
    {
      Id = userId,
      Email = "user@example.com"
    };
    var emptyRoles = new List<string>();

    _mockProfileRepo
        .Setup(x => x.GetUserById(userId))
        .ReturnsAsync(user);

    _mockAuthRepo
        .Setup(x => x.GetRolesAsync(user))
        .ReturnsAsync(emptyRoles);

    // Act
    var result = await _profileService.UpdateJWT(userId);

    // Assert
    result.Should().BeNull();
    _mockTokenService.Verify(x => x.GenerateToken(It.IsAny<ApiUser>(), It.IsAny<List<string>>()), Times.Never);
  }

  #endregion

  #region UpdateUserNewsletterPreferenceAsync Tests

  /// <summary>
  /// Verifies that updating newsletter preference successfully toggles the user in the cache.
  /// </summary>
  [Fact]
  public async Task UpdateUserNewsletterPreferenceAsync_WithValidUser_TogglesNewsletterInCache()
  {
    // Arrange
    var userId = "user123";
    var email = "user@example.com";
    var fullName = "John Doe";
    var successResult = IdentityResult.Success;

    _mockProfileRepo
        .Setup(x => x.UpdateUserNewsletterPreferenceAsync(userId))
        .ReturnsAsync((email, fullName, successResult));

    _mockEmailListCacheService
        .Setup(x => x.ToggleUserFromNewsletterAsync(It.IsAny<EmailNewsletterDTO>()))
        .Returns(Task.CompletedTask);

    // Act
    var result = await _profileService.UpdateUserNewsletterPreferenceAsync(userId);

    // Assert
    result.Should().Be(successResult);
    result.Succeeded.Should().BeTrue();
    _mockProfileRepo.Verify(x => x.UpdateUserNewsletterPreferenceAsync(userId), Times.Once);
    _mockEmailListCacheService.Verify(
        x => x.ToggleUserFromNewsletterAsync(It.Is<EmailNewsletterDTO>(
            dto => dto.Email == email && dto.FullName == fullName)),
        Times.Once);
  }

  /// <summary>
  /// Verifies that when newsletter preference update fails, the cache is not updated.
  /// </summary>
  [Fact]
  public async Task UpdateUserNewsletterPreferenceAsync_WhenUpdateFails_DoesNotToggleCache()
  {
    // Arrange
    var userId = "user123";
    var failureResult = IdentityResult.Failed(new IdentityError { Description = "User not found" });

    _mockProfileRepo
        .Setup(x => x.UpdateUserNewsletterPreferenceAsync(userId))
        .ReturnsAsync(("", "", failureResult));

    // Act
    var result = await _profileService.UpdateUserNewsletterPreferenceAsync(userId);

    // Assert
    result.Should().Be(failureResult);
    result.Succeeded.Should().BeFalse();
    _mockEmailListCacheService.Verify(
        x => x.ToggleUserFromNewsletterAsync(It.IsAny<EmailNewsletterDTO>()),
        Times.Never);
  }

  #endregion
}
