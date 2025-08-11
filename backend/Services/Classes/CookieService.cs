public class CookieService(IHttpContextAccessor httpContextAccessor, IConfiguration configuration) : ICookieService
{
  public void SetAuthCookie(string token)
  {
    var response = httpContextAccessor.HttpContext?.Response;
    if (response != null)
    {
      var expirationMinutes = int.Parse(configuration["JwtSettings:ExpirationInMinutes"] ?? "1440");
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