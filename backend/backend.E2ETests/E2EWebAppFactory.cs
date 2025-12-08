using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Configuration;
using Microsoft.EntityFrameworkCore;
using StackExchange.Redis;
using backend.Background;
using backend.Data;
using backend.MCP.Interfaces;
using Moq;

namespace backend.E2ETests;

/// <summary>
/// Custom WebApplicationFactory for E2E tests.
/// Uses SQLite in-memory database to support ExecuteUpdate operations.
/// Uses in-memory cache to avoid Redis dependency.
/// </summary>
public class E2EWebAppFactory : WebApplicationFactory<Program>
{
  private readonly string _connectionString = "DataSource=:memory:";
  private Microsoft.Data.Sqlite.SqliteConnection? _connection;

  protected override void ConfigureWebHost(IWebHostBuilder builder)
  {
    builder.UseEnvironment("Testing");

    builder.ConfigureAppConfiguration((context, config) =>
    {
      // Override configuration for E2E tests
      config.AddInMemoryCollection(new Dictionary<string, string?>
      {
        ["Authentication:Google:ClientId"] = "e2e-test-client-id",
        ["Authentication:Google:ClientSecret"] = "e2e-test-client-secret",
        ["JwtSettings:SecretKey"] = "DefaultSecretKeyForDevelopment123456789",
        ["JwtSettings:Issuer"] = "NotesGeneratorAPI",
        ["JwtSettings:Audience"] = "NotesGeneratorAPI",
        ["JwtSettings:ExpirationInMinutes"] = "60",
        ["OpenAI:ApiKey"] = Environment.GetEnvironmentVariable("OPENAI_API_KEY") ?? "test-api-key",
        ["MCP:YouTube:Token"] = Environment.GetEnvironmentVariable("YOUTUBE_API_KEY") ?? "test-youtube-token",
        ["N8N:NewsletterWebhookUrl"] = "https://test.n8n.webhook.url",
        ["CookieSettings:SecurePolicy"] = "None", // Allow HTTP cookies in tests
        ["CookieSettings:SameSiteMode"] = "Lax" // Lax mode for tests
      });
    });

    builder.ConfigureTestServices(services =>
    {
      // Use SQLite in-memory database for E2E tests - supports ExecuteUpdate unlike EF InMemory
      // Keep connection open for entire test lifecycle
      _connection = new Microsoft.Data.Sqlite.SqliteConnection(_connectionString);
      _connection.Open();

      services.AddDbContext<ApplicationDbContext>(options =>
        options.UseSqlite(_connection));

      // Remove NewsAggregationService - not needed for E2E tests
      var newsAggregationService = services.FirstOrDefault(d =>
        d.ServiceType == typeof(IHostedService) &&
        d.ImplementationType == typeof(NewsAggregationService));
      if (newsAggregationService != null)
      {
        services.Remove(newsAggregationService);
      }

      // Remove Redis distributed cache
      var redisCache = services.FirstOrDefault(d => d.ServiceType == typeof(IDistributedCache));
      if (redisCache != null)
      {
        services.Remove(redisCache);
      }

      var redisConnection = services.FirstOrDefault(d => d.ServiceType == typeof(IConnectionMultiplexer));
      if (redisConnection != null)
      {
        services.Remove(redisConnection);
      }

      // Remove MCP client service
      var mcpService = services.FirstOrDefault(d => d.ServiceType == typeof(IMcpClientService));
      if (mcpService != null)
      {
        services.Remove(mcpService);
      }

      // Add in-memory distributed cache
      services.AddDistributedMemoryCache();

      // Mock Redis IDatabase for starred messages and reported messages
      var mockDatabase = new Mock<StackExchange.Redis.IDatabase>();
      mockDatabase.Setup(d => d.ListRightPushAsync(It.IsAny<StackExchange.Redis.RedisKey>(), It.IsAny<StackExchange.Redis.RedisValue>(), It.IsAny<StackExchange.Redis.When>(), It.IsAny<StackExchange.Redis.CommandFlags>()))
        .ReturnsAsync(1);
      mockDatabase.Setup(d => d.ListRangeAsync(It.IsAny<StackExchange.Redis.RedisKey>(), It.IsAny<long>(), It.IsAny<long>(), It.IsAny<StackExchange.Redis.CommandFlags>()))
        .ReturnsAsync(Array.Empty<StackExchange.Redis.RedisValue>());
      mockDatabase.Setup(d => d.ListRemoveAsync(It.IsAny<StackExchange.Redis.RedisKey>(), It.IsAny<StackExchange.Redis.RedisValue>(), It.IsAny<long>(), It.IsAny<StackExchange.Redis.CommandFlags>()))
        .ReturnsAsync(1);

      var mockRedis = new Mock<IConnectionMultiplexer>();
      mockRedis.Setup(r => r.GetDatabase(It.IsAny<int>(), It.IsAny<object>())).Returns(mockDatabase.Object);
      services.AddSingleton(mockRedis.Object);

      // Mock MCP client
      var mockMcpClient = new Mock<IMcpClientService>();
      mockMcpClient.Setup(m => m.InitializeAsync()).Returns(Task.CompletedTask);
      mockMcpClient.Setup(m => m.GetEssentialTools()).Returns(new List<ModelContextProtocol.Client.McpClientTool>());
      mockMcpClient.Setup(m => m.GetAllAvailableToolsAsync()).Returns(new List<ModelContextProtocol.Client.McpClientTool>());
      services.AddSingleton(mockMcpClient.Object);

      // Override CookieService to use test-friendly cookie settings (no Secure flag for HTTP)
      var cookieService = services.FirstOrDefault(d => d.ServiceType == typeof(backend.Services.Interfaces.Auth.ICookieService));
      if (cookieService != null)
      {
        services.Remove(cookieService);
      }
      services.AddScoped<backend.Services.Interfaces.Auth.ICookieService, TestCookieService>();
    });
  }

  protected override void ConfigureClient(HttpClient client)
  {
    base.ConfigureClient(client);
    client.Timeout = TimeSpan.FromSeconds(30);
  }

  protected override IHost CreateHost(IHostBuilder builder)
  {
    var host = base.CreateHost(builder);

    using (var scope = host.Services.CreateScope())
    {
      var services = scope.ServiceProvider;
      var db = services.GetRequiredService<ApplicationDbContext>();
      var roleManager = services.GetRequiredService<Microsoft.AspNetCore.Identity.RoleManager<Microsoft.AspNetCore.Identity.IdentityRole>>();

      db.Database.EnsureCreated();

      var roles = new[] { "ADMIN", "PREMIUM", "USER" };
      foreach (var role in roles)
      {
        if (!roleManager.RoleExistsAsync(role).GetAwaiter().GetResult())
        {
          roleManager.CreateAsync(new Microsoft.AspNetCore.Identity.IdentityRole(role)).GetAwaiter().GetResult();
        }
      }
    }

    return host;
  }

  protected override void Dispose(bool disposing)
  {
    if (disposing)
    {
      _connection?.Close();
      _connection?.Dispose();
    }
    base.Dispose(disposing);
  }
}
