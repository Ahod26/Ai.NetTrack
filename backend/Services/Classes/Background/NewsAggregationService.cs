using backend.MCP.Interfaces;
using backend.Services.Interfaces.NewsAggregation;

namespace backend.Services.Background;

public class NewsAggregationService(
  IServiceScopeFactory scopeFactory,
  ILogger<NewsAggregationService> logger) : BackgroundService
{
  protected override async Task ExecuteAsync(CancellationToken cancellationToken)
  {
    await Task.Delay(TimeSpan.FromSeconds(3), cancellationToken);

    while (!cancellationToken.IsCancellationRequested)
    {
      try
      {
        logger.LogInformation("Starting news aggregation...");

        await using var scope = scopeFactory.CreateAsyncScope();
        var newsCollector = scope.ServiceProvider.GetRequiredService<INewsCollectorService>();
        var mcpClientService = scope.ServiceProvider.GetRequiredService<IMcpClientService>();

        await mcpClientService.InitializeAsync();

        await newsCollector.CollectAllNews();

        logger.LogInformation("News aggregation completed successfully");
        await Task.Delay(TimeSpan.FromDays(1), cancellationToken); 
      }
      catch (Exception ex)
      {
        logger.LogError(ex, "Error in news aggregation, retrying in 30 minutes");
        await Task.Delay(TimeSpan.FromMinutes(30), cancellationToken);
      }
    }
  }
}
