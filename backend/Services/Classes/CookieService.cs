using Microsoft.Extensions.Options;
using backend.Models.Configuration;
using backend.Services.Interfaces;

namespace backend.Services.Classes;

public class CookieService(IHttpContextAccessor httpContextAccessor, IOptions<JwtSettings> options) : ICookieService
{
  private readonly JwtSettings settings = options.Value;
  public void SetAuthCookie(string token)
  {
    var response = httpContextAccessor.HttpContext?.Response;
    if (response != null)
    {
      var expirationMinutes = settings.ExpirationInMinutes;
      var cookieOptions = new CookieOptions
      {
        HttpOnly = true,
        Secure = true,
        SameSite = SameSiteMode.None,
        Expires = DateTimeOffset.UtcNow.AddMinutes(expirationMinutes)
      };
      response.Cookies.Append("authToken", token, cookieOptions);
    }
  }
}