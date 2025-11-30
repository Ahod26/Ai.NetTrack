using backend.Background.Interfaces;
using backend.MCP.Interfaces;
namespace backend.Background;

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
        var N8NIntegration = scope.ServiceProvider.GetRequiredService<IN8NIntegration>();
        var mcpClientService = scope.ServiceProvider.GetRequiredService<IMcpClientService>();

        await mcpClientService.InitializeAsync();

        int newsCount = await newsCollector.CollectAllNews();
        logger.LogInformation("News aggregation completed successfully");

        if (newsCount > 0)
          await N8NIntegration.SendUsersTodayNewsAsync();
        
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