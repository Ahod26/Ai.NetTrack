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

namespace backend.IntegrationTests;

public class WebAppFactory : WebApplicationFactory<Program>
{
  private readonly string _databaseName = Guid.NewGuid().ToString();

  protected override void ConfigureWebHost(IWebHostBuilder builder)
  {
    // Set environment BEFORE configuration to ensure DatabaseExtensions sees it
    builder.UseEnvironment("Testing");

    builder.ConfigureAppConfiguration((context, config) =>
    {
      // Add test configuration to override OAuth settings
      config.AddInMemoryCollection(new Dictionary<string, string?>
      {
        ["Authentication:Google:ClientId"] = "test-client-id",
        ["Authentication:Google:ClientSecret"] = "test-client-secret",
        ["JwtSettings:SecretKey"] = "TestSecretKey123456789012345678901234567890",
        ["JwtSettings:Issuer"] = "TestIssuer",
        ["JwtSettings:Audience"] = "TestAudience",
        ["OpenAI:ApiKey"] = "test-api-key-for-integration-tests-only",
        ["MCP:YouTube:Token"] = "test-youtube-token",
        ["N8N:NewsletterWebhookUrl"] = "https://test.n8n.webhook.url"
      });
    });

    builder.ConfigureTestServices(services =>
    {
      // Add in-memory database with unique name per test class
      services.AddDbContext<ApplicationDbContext>(options =>
        options.UseInMemoryDatabase(_databaseName));

      // Remove NewsAggregationService to speed up tests
      var newsAggregationService = services.FirstOrDefault(d =>
        d.ServiceType == typeof(IHostedService) &&
        d.ImplementationType == typeof(NewsAggregationService));
      if (newsAggregationService != null)
      {
        services.Remove(newsAggregationService);
      }

      // Remove Redis services that cause timeouts
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

      // Remove McpClientService that tries to initialize Docker and NPX
      var mcpService = services.FirstOrDefault(d => d.ServiceType == typeof(IMcpClientService));
      if (mcpService != null)
      {
        services.Remove(mcpService);
      }

      // Add in-memory distributed cache for tests
      services.AddDistributedMemoryCache();

      // Add mock ConnectionMultiplexer and IDatabase for RedisCacheRepo
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

      // Add mock MCP client that doesn't do any initialization
      var mockMcpClient = new Mock<IMcpClientService>();
      mockMcpClient.Setup(m => m.InitializeAsync()).Returns(Task.CompletedTask);
      mockMcpClient.Setup(m => m.GetEssentialTools()).Returns(new List<ModelContextProtocol.Client.McpClientTool>());
      mockMcpClient.Setup(m => m.GetAllAvailableToolsAsync()).Returns(new List<ModelContextProtocol.Client.McpClientTool>());
      services.AddSingleton(mockMcpClient.Object);
    });
  }

  protected override void ConfigureClient(HttpClient client)
  {
    base.ConfigureClient(client);
    // Set timeout to 10 seconds to prevent hanging tests
    client.Timeout = TimeSpan.FromSeconds(10);
  }

  protected override IHost CreateHost(IHostBuilder builder)
  {
    var host = base.CreateHost(builder);

    // Initialize the database and seed roles
    using (var scope = host.Services.CreateScope())
    {
      var services = scope.ServiceProvider;
      var db = services.GetRequiredService<ApplicationDbContext>();
      var roleManager = services.GetRequiredService<Microsoft.AspNetCore.Identity.RoleManager<Microsoft.AspNetCore.Identity.IdentityRole>>();

      db.Database.EnsureCreated();

      // Seed roles if they don't exist
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


}
