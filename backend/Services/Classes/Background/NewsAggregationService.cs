using backend.Services.Interfaces.NewsAggregation;

namespace backend.Services.Background;

public class NewsAggregationService(
  IServiceScopeFactory scopeFactory,
  ILogger<NewsAggregationService> logger) : BackgroundService
{
  protected override async Task ExecuteAsync(CancellationToken cancellationToken)
  {
    while (!cancellationToken.IsCancellationRequested)
    {
      try
      {
        using var scope = scopeFactory.CreateScope();
        var newsCollector = scope.ServiceProvider.GetRequiredService<INewsCollectorService>();

        await newsCollector.CollectAllNews();

        await Task.Delay(TimeSpan.FromDays(1), cancellationToken);
      }
      catch (Exception ex)
      {
        logger.LogError(ex, "Error in news aggregation");
        await Task.Delay(TimeSpan.FromMinutes(30), cancellationToken);
      }
    }
  }
}
