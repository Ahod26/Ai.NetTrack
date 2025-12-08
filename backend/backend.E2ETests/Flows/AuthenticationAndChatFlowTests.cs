using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Xunit;
using backend.E2ETests.Helpers;
using Microsoft.AspNetCore.SignalR.Client;
using backend.Models.Dtos;

namespace backend.E2ETests.Flows;

/// <summary>
/// E2E tests for the complete authentication and chat creation flow.
/// Tests: Register → Login → Create Chat → SignalR Connection
/// </summary>
public class AuthenticationAndChatFlowTests : IClassFixture<E2EWebAppFactory>
{
  private readonly E2EWebAppFactory _factory;
  private readonly HttpClient _client;

  public AuthenticationAndChatFlowTests(E2EWebAppFactory factory)
  {
    _factory = factory;
    // Create client with cookie persistence
    var options = new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
    {
      AllowAutoRedirect = false,
      HandleCookies = true
    };
    _client = factory.CreateClient(options);
  }

  /// <summary>
  /// Complete authentication flow: Register new user → Login → Check auth status
  /// </summary>
  [Fact]
  public async Task CompleteAuthenticationFlow_RegisterLoginAndCheckStatus_Success()
  {
    // Arrange
    var email = $"e2euser{Guid.NewGuid()}@test.com";
    var password = "SecurePass123!";
    var fullName = "E2E Test User";

    // Act 1: Register
    var registerDto = new RegisterDTO
    {
      Email = email,
      Password = password,
      FullName = fullName,
      IsSubscribedToNewsletter = false
    };

    var registerResponse = await _client.PostAsJsonAsync("/auth", registerDto);

    // Assert registration
    registerResponse.StatusCode.Should().Be(HttpStatusCode.OK);
    var registerResult = await registerResponse.Content.ReadFromJsonAsync<RegisterResponse>();
    registerResult.Should().NotBeNull();
    registerResult!.Success.Should().BeTrue();

    // Act 2: Login
    var loginDto = new LoginDTO
    {
      Email = email,
      Password = password
    };

    var loginResponse = await _client.PostAsJsonAsync("/auth/login", loginDto);

    // Assert login
    loginResponse.StatusCode.Should().Be(HttpStatusCode.OK);
    var loginResult = await loginResponse.Content.ReadFromJsonAsync<LoginResponse>();
    loginResult.Should().NotBeNull();
    loginResult!.Success.Should().BeTrue();
    loginResult.UserInfo.Should().NotBeNull();
    loginResult.UserInfo!.ApiUserDto!.Email.Should().Be(email);
    loginResult.UserInfo!.ApiUserDto!.FullName.Should().Be(fullName);

    // Generate JWT token for subsequent authenticated requests
    var token = await TokenHelper.GetTokenForUserAsync(_factory, email);
    _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

    // Act 3: Check auth status (should be authenticated with Bearer token)
    var statusResponse = await _client.GetAsync("/auth/status");

    // Assert status
    statusResponse.StatusCode.Should().Be(HttpStatusCode.OK);
    var statusResult = await statusResponse.Content.ReadFromJsonAsync<AuthStatusResponse>();
    statusResult.Should().NotBeNull();
    statusResult!.IsAuthenticated.Should().BeTrue();
    statusResult.User.Should().NotBeNull();
  }

  /// <summary>
  /// Test login with wrong password returns unauthorized
  /// </summary>
  [Fact]
  public async Task LoginWithWrongPassword_ReturnsUnauthorized()
  {
    // Arrange - Create user first
    var (client, email, _, _) = await TestUserHelper.CreateAuthenticatedUserAsync(_factory);

    // Act - Try to login with wrong password using a fresh client
    var freshClient = _factory.CreateClient();
    var wrongLoginDto = new LoginDTO
    {
      Email = email,
      Password = "WrongPassword123!"
    };

    var response = await freshClient.PostAsJsonAsync("/auth/login", wrongLoginDto);

    // Assert
    response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
  }

  /// <summary>
  /// Test logout deletes authentication cookie
  /// </summary>
  [Fact]
  public async Task Logout_DeletesAuthenticationCookie_Success()
  {
    // Arrange - Create and login user
    var (client, _, _, _) = await TestUserHelper.CreateAuthenticatedUserAsync(_factory);

    // Verify user is authenticated
    var statusBeforeResponse = await client.GetAsync("/auth/status");
    if (statusBeforeResponse.StatusCode != HttpStatusCode.OK)
    {
      var errorContent = await statusBeforeResponse.Content.ReadAsStringAsync();
      Console.WriteLine($"[DEBUG] Status check failed. Response: {errorContent}");
      Console.WriteLine($"[DEBUG] Auth header: {client.DefaultRequestHeaders.Authorization}");
    }
    statusBeforeResponse.StatusCode.Should().Be(HttpStatusCode.OK);

    // Act - Logout
    var logoutResponse = await client.PostAsync("/auth/logout", null);

    // Assert
    logoutResponse.StatusCode.Should().Be(HttpStatusCode.OK);

    // Remove Bearer token to simulate full logout (logout only clears cookies, not headers)
    client.DefaultRequestHeaders.Authorization = null;

    // Verify user is no longer authenticated
    var statusAfterResponse = await client.GetAsync("/auth/status");
    statusAfterResponse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
  }

  /// <summary>
  /// Test authenticated user can create a chat
  /// </summary>
  [Fact]
  public async Task AuthenticatedUser_CanCreateChat_Success()
  {
    // Arrange
    var (client, _, _, _) = await TestUserHelper.CreateAuthenticatedUserAsync(_factory);

    // Act - Create chat
    var createChatDto = new CreateChatDTO
    {
      FirstUserMessage = "Hello, this is my first message!"
    };

    var response = await client.PostAsJsonAsync("/chat", createChatDto);

    // Assert
    response.StatusCode.Should().Be(HttpStatusCode.OK);
    var chatResult = await response.Content.ReadFromJsonAsync<ChatResponse>();
    chatResult.Should().NotBeNull();
    chatResult!.Id.Should().NotBeEmpty();
    chatResult.Title.Should().NotBeNullOrEmpty();
  }

  /// <summary>
  /// Test unauthenticated user cannot create chat
  /// </summary>
  [Fact]
  public async Task UnauthenticatedUser_CannotCreateChat_ReturnsUnauthorized()
  {
    // Arrange
    var unauthenticatedClient = _factory.CreateClient();

    // Act
    var createChatDto = new CreateChatDTO
    {
      FirstUserMessage = "Hello!"
    };

    var response = await unauthenticatedClient.PostAsJsonAsync("/chat", createChatDto);

    // Assert
    response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
  }

  /// <summary>
  /// Test authenticated user can get their chats
  /// </summary>
  [Fact]
  public async Task AuthenticatedUser_CanGetTheirChats_Success()
  {
    // Arrange
    var (client, _, _, _) = await TestUserHelper.CreateAuthenticatedUserAsync(_factory);

    // Create a chat first
    var createChatDto = new CreateChatDTO
    {
      FirstUserMessage = "Test chat message"
    };
    await client.PostAsJsonAsync("/chat", createChatDto);

    // Act - Get chats
    var response = await client.GetAsync("/chat");

    // Assert
    response.StatusCode.Should().Be(HttpStatusCode.OK);
    var chats = await response.Content.ReadFromJsonAsync<List<ChatMetadataDto>>();
    chats.Should().NotBeNull();
    chats.Should().NotBeEmpty();
    chats![0].Title.Should().NotBeNullOrEmpty();
  }

  #region DTOs

  public class RegisterResponse
  {
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public List<string> Errors { get; set; } = new();
  }

  public class LoginResponse
  {
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public UserInfoDto? UserInfo { get; set; }
  }

  public class UserInfoDto
  {
    public string Id { get; set; } = string.Empty;
    public ApiUserDto? ApiUserDto { get; set; }
    public List<string> Roles { get; set; } = new();
  }

  public class ApiUserDto
  {
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public bool IsSubscribedToNewsletter { get; set; }
  }

  public class AuthStatusResponse
  {
    public bool IsAuthenticated { get; set; }
    public UserInfoDto? User { get; set; }
  }

  public class CreateChatDTO
  {
    public string FirstUserMessage { get; set; } = string.Empty;
    public string? RelatedNewsURL { get; set; }
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
    public DateTime LastMessageAt { get; set; }
    public bool IsContextFull { get; set; }
  }

  #endregion
}
