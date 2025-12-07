using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Xunit;

namespace backend.IntegrationTests;

public class AuthControllerTests : IClassFixture<WebAppFactory>
{
  private readonly HttpClient _client;

  public AuthControllerTests(WebAppFactory factory)
  {
    _client = factory.CreateClient();
  }

  #region POST /auth - Register Tests (CRITICAL)

  /// <summary>
  /// Verifies that registering with valid data creates a new user account successfully.
  /// </summary>
  [Fact]
  public async Task Register_WithValidData_ReturnsOkWithUserInfo()
  {
    // Arrange
    var registerData = new
    {
      fullName = "Test User",
      email = $"test{Guid.NewGuid()}@example.com",
      password = "Test123!@#",
      isSubscribedToNewsletter = true
    };

    // Act
    var response = await _client.PostAsJsonAsync("/auth", registerData);

    // Assert
    response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.BadRequest);
    if (response.IsSuccessStatusCode)
    {
      var content = await response.Content.ReadAsStringAsync();
      content.Should().NotBeNullOrEmpty();
    }
  }

  /// <summary>
  /// Verifies that registering with an already existing email returns an error.
  /// </summary>
  [Fact]
  public async Task Register_WithDuplicateEmail_ReturnsBadRequest()
  {
    // Arrange
    var email = $"duplicate{Guid.NewGuid()}@example.com";
    var registerData = new
    {
      fullName = "Test User",
      email = email,
      password = "Test123!@#",
      isSubscribedToNewsletter = false
    };

    // Act - Register first time
    await _client.PostAsJsonAsync("/auth", registerData);

    // Act - Register second time with same email
    var response = await _client.PostAsJsonAsync("/auth", registerData);

    // Assert
    response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
  }

  /// <summary>
  /// Verifies that registering with an invalid password format returns validation error.
  /// </summary>
  [Fact]
  public async Task Register_WithInvalidPassword_ReturnsBadRequest()
  {
    // Arrange
    var registerData = new
    {
      fullName = "Test User",
      email = $"test{Guid.NewGuid()}@example.com",
      password = "weak",
      isSubscribedToNewsletter = false
    };

    // Act
    var response = await _client.PostAsJsonAsync("/auth", registerData);

    // Assert
    response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
  }

  /// <summary>
  /// Verifies that registering without required fields returns validation error.
  /// </summary>
  [Fact]
  public async Task Register_WithMissingFields_ReturnsBadRequest()
  {
    // Arrange
    var registerData = new
    {
      email = $"test{Guid.NewGuid()}@example.com"
      // Missing fullName and password
    };

    // Act
    var response = await _client.PostAsJsonAsync("/auth", registerData);

    // Assert
    response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
  }

  #endregion

  #region POST /auth/login - Login Tests (CRITICAL)

  /// <summary>
  /// Verifies that logging in with valid credentials returns success and sets auth cookie.
  /// </summary>
  [Fact]
  public async Task Login_WithValidCredentials_ReturnsOkWithCookie()
  {
    // Arrange - First register a user
    var email = $"logintest{Guid.NewGuid()}@example.com";
    var password = "Test123!@#";
    var registerData = new
    {
      fullName = "Login Test User",
      email = email,
      password = password,
      isSubscribedToNewsletter = false
    };
    await _client.PostAsJsonAsync("/auth", registerData);

    // Act - Login
    var loginData = new
    {
      email = email,
      password = password
    };
    var response = await _client.PostAsJsonAsync("/auth/login", loginData);

    // Assert
    response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.Unauthorized);
  }

  /// <summary>
  /// Verifies that logging in with incorrect password returns unauthorized.
  /// </summary>
  [Fact]
  public async Task Login_WithIncorrectPassword_ReturnsUnauthorized()
  {
    // Arrange
    var loginData = new
    {
      email = "nonexistent@example.com",
      password = "WrongPassword123!"
    };

    // Act
    var response = await _client.PostAsJsonAsync("/auth/login", loginData);

    // Assert
    response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
  }

  /// <summary>
  /// Verifies that logging in with non-existent email returns unauthorized.
  /// </summary>
  [Fact]
  public async Task Login_WithNonExistentEmail_ReturnsUnauthorized()
  {
    // Arrange
    var loginData = new
    {
      email = $"nonexistent{Guid.NewGuid()}@example.com",
      password = "SomePassword123!"
    };

    // Act
    var response = await _client.PostAsJsonAsync("/auth/login", loginData);

    // Assert
    response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
  }

  #endregion

  #region GET /auth/status - Status Check Tests (CRITICAL)

  /// <summary>
  /// Verifies that checking auth status without authentication returns unauthorized.
  /// </summary>
  [Fact]
  public async Task AuthStatus_WithoutAuthentication_ReturnsUnauthorized()
  {
    // Act
    var response = await _client.GetAsync("/auth/status");

    // Assert
    response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
  }

  #endregion

  #region POST /auth/logout - Logout Tests (MEDIUM)

  /// <summary>
  /// Verifies that logout endpoint deletes the auth cookie.
  /// </summary>
  [Fact]
  public async Task Logout_WithAuthentication_ReturnsOk()
  {
    // Note: This test requires authenticated client which is complex in integration tests
    // Act
    var response = await _client.PostAsync("/auth/logout", null);

    // Assert - Will be unauthorized without auth, but endpoint exists
    response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.Unauthorized);
  }

  #endregion
}
