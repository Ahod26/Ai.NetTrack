namespace backend.Services.Interfaces.Auth;

public interface ICookieService
{
  void SetAuthCookie(string token);
}