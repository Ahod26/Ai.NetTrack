namespace backend.Services.Interfaces;

public interface ICookieService
{
  void SetAuthCookie(string token);
}