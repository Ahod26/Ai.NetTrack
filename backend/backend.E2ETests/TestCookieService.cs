using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using backend.Models.Configuration;
using backend.Services.Interfaces.Auth;

namespace backend.E2ETests;

/// <summary>
/// Test-friendly cookie service that doesn't require HTTPS
/// </summary>
public class TestCookieService(IHttpContextAccessor httpContextAccessor, IOptions<JwtSettings> options) : ICookieService
{
  private readonly JwtSettings settings = options.Value;

  public void SetAuthCookie(string token)
  {
    Console.WriteLine($"[TestCookieService] SetAuthCookie called with token length: {token?.Length ?? 0}");
    var response = httpContextAccessor.HttpContext?.Response;
    if (response != null)
    {
      var expirationMinutes = settings.ExpirationInMinutes;
      var cookieOptions = new CookieOptions
      {
        HttpOnly = true,
        Secure = false, // Allow HTTP in tests (production uses true)
        SameSite = SameSiteMode.Lax, // Test-friendly (production uses None)
        Expires = DateTimeOffset.UtcNow.AddMinutes(expirationMinutes)
      };
      response.Cookies.Append("authToken", token, cookieOptions);
      Console.WriteLine($"[TestCookieService] Cookie set: HttpOnly={cookieOptions.HttpOnly}, Secure={cookieOptions.Secure}, SameSite={cookieOptions.SameSite}");
    }
    else
    {
      Console.WriteLine("[TestCookieService] ERROR: HttpContext.Response is null!");
    }
  }
}
