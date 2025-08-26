using Microsoft.Extensions.Options;

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