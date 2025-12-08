using System.Net;

namespace backend.E2ETests.Helpers;

/// <summary>
/// HTTP message handler that captures and forwards cookies from Set-Cookie headers
/// </summary>
public class CookieHandler : DelegatingHandler
{
  private readonly CookieContainer _cookies = new();

  public CookieHandler(HttpMessageHandler innerHandler) : base(innerHandler)
  {
  }

  protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
  {
    // Add cookies from container to request
    var cookieHeader = _cookies.GetCookieHeader(request.RequestUri!);
    if (!string.IsNullOrEmpty(cookieHeader))
    {
      Console.WriteLine($"[CookieHandler] Adding cookies to {request.RequestUri?.PathAndQuery}: {cookieHeader}");
      request.Headers.Add("Cookie", cookieHeader);
    }
    else
    {
      Console.WriteLine($"[CookieHandler] No cookies to add for {request.RequestUri?.PathAndQuery}");
    }

    // Send request
    var response = await base.SendAsync(request, cancellationToken);

    // Extract cookies from response and add to container
    if (response.Headers.TryGetValues("Set-Cookie", out var setCookies))
    {
      Console.WriteLine($"[CookieHandler] Found Set-Cookie headers in response: {string.Join(", ", setCookies)}");
      foreach (var setCookie in setCookies)
      {
        _cookies.SetCookies(request.RequestUri!, setCookie);
        Console.WriteLine($"[CookieHandler] Added cookie from: {setCookie}");
      }
    }
    else
    {
      Console.WriteLine($"[CookieHandler] No Set-Cookie headers in response from {request.RequestUri?.PathAndQuery}");
    }

    return response;
  }
}
