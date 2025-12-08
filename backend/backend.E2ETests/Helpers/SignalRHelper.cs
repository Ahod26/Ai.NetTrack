using Microsoft.AspNetCore.SignalR.Client;

namespace backend.E2ETests.Helpers;

/// <summary>
/// Helper class for managing SignalR connections in E2E tests.
/// </summary>
public class SignalRHelper
{
  private HubConnection? _connection;
  private readonly string _hubUrl;
  private readonly HttpClient _authenticatedClient;

  public SignalRHelper(string hubUrl, HttpClient authenticatedClient)
  {
    _hubUrl = hubUrl;
    _authenticatedClient = authenticatedClient;
  }

  public async Task<HubConnection> CreateConnectionAsync()
  {
    // Extract cookies from authenticated HttpClient
    var cookieContainer = new System.Net.CookieContainer();
    if (_authenticatedClient.DefaultRequestHeaders.TryGetValues("Cookie", out var cookies))
    {
      foreach (var cookie in cookies)
      {
        cookieContainer.SetCookies(new Uri(_hubUrl), cookie);
      }
    }

    _connection = new HubConnectionBuilder()
      .WithUrl(_hubUrl, options =>
      {
        options.HttpMessageHandlerFactory = _ => new HttpClientHandler
        {
          CookieContainer = cookieContainer
        };
        options.Cookies = cookieContainer;
      })
      .WithAutomaticReconnect()
      .Build();

    await _connection.StartAsync();
    return _connection;
  }

  public HubConnection Connection => _connection ?? throw new InvalidOperationException("Connection not created");

  public async Task DisposeAsync()
  {
    if (_connection != null)
    {
      await _connection.DisposeAsync();
    }
  }
}
