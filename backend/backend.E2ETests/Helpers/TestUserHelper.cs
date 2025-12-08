using System.Net.Http.Json;
using backend.Models.Dtos;

namespace backend.E2ETests.Helpers;

/// <summary>
/// Helper class for managing test users in E2E tests.
/// </summary>
public static class TestUserHelper
{
  /// <summary>
  /// Creates a new test user with a unique email and returns the authenticated HTTP client.
  /// Uses JWT Bearer token extracted from login response.
  /// </summary>
  public static async Task<(HttpClient client, string email, string password, string userId)> CreateAuthenticatedUserAsync(E2EWebAppFactory factory)
  {
    var client = factory.CreateClient();

    var email = $"testuser{Guid.NewGuid()}@test.com";
    var password = "TestPass123!";
    var fullName = "Test User";

    // Register user
    var registerDto = new RegisterDTO
    {
      Email = email,
      Password = password,
      FullName = fullName,
      IsSubscribedToNewsletter = false
    };

    var registerResponse = await client.PostAsJsonAsync("/auth", registerDto);
    registerResponse.EnsureSuccessStatusCode();

    // Login to get JWT token
    var loginDto = new LoginDTO
    {
      Email = email,
      Password = password
    };

    var loginResponse = await client.PostAsJsonAsync("/auth/login", loginDto);
    loginResponse.EnsureSuccessStatusCode();

    var loginResult = await loginResponse.Content.ReadFromJsonAsync<LoginResponse>();
    var userId = loginResult!.UserInfo!.Id;

    // Generate JWT token for E2E authentication
    try
    {
      var token = await TokenHelper.GetTokenForUserAsync(factory, email);
      Console.WriteLine($"[TestUserHelper] Generated token length: {token.Length}");
      client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
      Console.WriteLine($"[TestUserHelper] Authorization header set: {client.DefaultRequestHeaders.Authorization}");
    }
    catch (Exception ex)
    {
      Console.WriteLine($"[TestUserHelper] ERROR generating token: {ex.Message}");
      throw;
    }

    return (client, email, password, userId);
  }

  /// <summary>
  /// Login response DTO for E2E tests
  /// </summary>
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
}
