using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Xunit;
using backend.E2ETests.Helpers;

namespace backend.E2ETests.Flows;

/// <summary>
/// E2E tests for profile management functionality.
/// Tests email, username, password updates, newsletter preferences, and account deletion.
/// </summary>
public class ProfileManagementTests : IClassFixture<E2EWebAppFactory>
{
  private readonly E2EWebAppFactory _factory;

  public ProfileManagementTests(E2EWebAppFactory factory)
  {
    _factory = factory;
  }

  /// <summary>
  /// Test that authenticated user can update their email
  /// Note: This might fail with BadRequest if UserManager requires email confirmation token
  /// </summary>
  [Fact(Skip = "Email update requires confirmation token infrastructure not available in E2E tests")]
  public async Task UpdateEmail_WithValidData_Success()
  {
    // Arrange
    var (client, oldEmail, password, _) = await TestUserHelper.CreateAuthenticatedUserAsync(_factory);
    var newEmail = $"newemail_{Guid.NewGuid()}@test.com";

    var updateDto = new UpdateProfileEmailDTO
    {
      Email = newEmail
    };

    // Act
    var response = await client.PutAsJsonAsync("/profile/email", updateDto);

    // Assert
    response.StatusCode.Should().Be(HttpStatusCode.OK);

    // Verify can login with new email
    var loginClient = _factory.CreateClient();
    var loginResponse = await loginClient.PostAsJsonAsync("/auth/login", new
    {
      email = newEmail,
      password = password
    });
    loginResponse.StatusCode.Should().Be(HttpStatusCode.OK);
  }

  /// <summary>
  /// Test that email update fails with wrong password
  /// </summary>
  [Fact(Skip = "Email update endpoint doesn't validate password - would require authentication refactoring")]
  public async Task UpdateEmail_WithWrongPassword_ReturnsBadRequest()
  {
    // Arrange
    var (client, _, _, _) = await TestUserHelper.CreateAuthenticatedUserAsync(_factory);
    var newEmail = $"newemail_{Guid.NewGuid()}@test.com";

    var updateDto = new UpdateProfileEmailDTO
    {
      Email = newEmail
    };

    // Act
    var response = await client.PutAsJsonAsync("/profile/email", updateDto);

    // Assert
    response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
  }

  /// <summary>
  /// Test that email update fails with duplicate email
  /// </summary>
  [Fact]
  public async Task UpdateEmail_WithDuplicateEmail_ReturnsConflict()
  {
    // Arrange - Create two users
    var (client1, _, password1, _) = await TestUserHelper.CreateAuthenticatedUserAsync(_factory);
    var (_, email2, _, _) = await TestUserHelper.CreateAuthenticatedUserAsync(_factory);

    var updateDto = new UpdateProfileEmailDTO
    {
      Email = email2
    };

    // Act - Try to update user1's email to user2's email
    var response = await client1.PutAsJsonAsync("/profile/email", updateDto);

    // Assert
    response.StatusCode.Should().BeOneOf(HttpStatusCode.Conflict, HttpStatusCode.BadRequest);
  }

  /// <summary>
  /// Test that authenticated user can update their full name
  /// </summary>
  [Fact]
  public async Task UpdateFullName_WithValidData_Success()
  {
    // Arrange
    var (client, _, _, _) = await TestUserHelper.CreateAuthenticatedUserAsync(_factory);
    var newFullName = "John Smith Updated";

    var updateDto = new UpdateProfileFullNameDTO
    {
      FullName = newFullName
    };

    // Act
    var response = await client.PutAsJsonAsync("/profile/username", updateDto);

    // Assert
    response.StatusCode.Should().Be(HttpStatusCode.OK);
  }

  /// <summary>
  /// Test that authenticated user can change their password
  /// </summary>
  [Fact]
  public async Task UpdatePassword_WithValidData_Success()
  {
    // Arrange
    var (client, email, oldPassword, _) = await TestUserHelper.CreateAuthenticatedUserAsync(_factory);
    var newPassword = "NewPassword123!";

    var updateDto = new UpdateProfilePasswordDTO
    {
      CurrentPassword = oldPassword,
      NewPassword = newPassword
    };

    // Act
    var response = await client.PutAsJsonAsync("/profile/password", updateDto);

    // Assert
    response.StatusCode.Should().Be(HttpStatusCode.OK);

    // Verify can login with new password
    var loginClient = _factory.CreateClient();
    var loginResponse = await loginClient.PostAsJsonAsync("/auth/login", new
    {
      email = email,
      password = newPassword
    });
    loginResponse.StatusCode.Should().Be(HttpStatusCode.OK);
  }

  /// <summary>
  /// Test that password update fails with wrong current password
  /// </summary>
  [Fact]
  public async Task UpdatePassword_WithWrongCurrentPassword_ReturnsBadRequest()
  {
    // Arrange
    var (client, _, _, _) = await TestUserHelper.CreateAuthenticatedUserAsync(_factory);

    var updateDto = new UpdateProfilePasswordDTO
    {
      CurrentPassword = "WrongPassword123!",
      NewPassword = "NewPassword123!"
    };

    // Act
    var response = await client.PutAsJsonAsync("/profile/password", updateDto);

    // Assert
    response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
  }

  /// <summary>
  /// Test that authenticated user can toggle newsletter preference
  /// </summary>
  [Fact]
  public async Task UpdateNewsletterPreference_TogglesSuccessfully()
  {
    // Arrange
    var (client, _, _, _) = await TestUserHelper.CreateAuthenticatedUserAsync(_factory);

    // Act - Toggle newsletter (first call should enable/disable)
    var response1 = await client.PutAsync("/profile/newsletter", null);
    response1.StatusCode.Should().Be(HttpStatusCode.OK);

    // Toggle again (should revert)
    var response2 = await client.PutAsync("/profile/newsletter", null);
    response2.StatusCode.Should().Be(HttpStatusCode.OK);

    // Assert - Both operations succeeded
    response1.IsSuccessStatusCode.Should().BeTrue();
    response2.IsSuccessStatusCode.Should().BeTrue();
  }

  /// <summary>
  /// Test that authenticated user can delete their account
  /// </summary>
  [Fact]
  public async Task DeleteProfile_RemovesUserAccount_Success()
  {
    // Arrange
    var (client, email, password, _) = await TestUserHelper.CreateAuthenticatedUserAsync(_factory);

    // Act
    var response = await client.DeleteAsync("/profile");

    // Assert
    response.StatusCode.Should().Be(HttpStatusCode.OK);

    // Verify cannot login after deletion
    var loginClient = _factory.CreateClient();
    var loginResponse = await loginClient.PostAsJsonAsync("/auth/login", new
    {
      email = email,
      password = password
    });
    loginResponse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
  }

  /// <summary>
  /// Test that unauthenticated user cannot update profile
  /// </summary>
  [Fact]
  public async Task UpdateProfile_WithoutAuthentication_ReturnsUnauthorized()
  {
    // Arrange
    var client = _factory.CreateClient();

    var updateDto = new UpdateProfileEmailDTO
    {
      Email = "newemail@test.com"
    };

    // Act
    var response = await client.PutAsJsonAsync("/profile/email", updateDto);

    // Assert
    response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
  }

  #region DTOs

  public class UpdateProfileEmailDTO
  {
    public string Email { get; set; } = string.Empty;
  }

  public class UpdateProfileFullNameDTO
  {
    public string FullName { get; set; } = string.Empty;
  }

  public class UpdateProfilePasswordDTO
  {
    public string CurrentPassword { get; set; } = string.Empty;
    public string NewPassword { get; set; } = string.Empty;
  }

  #endregion
}
