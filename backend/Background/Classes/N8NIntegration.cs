using backend.Background.Interfaces;
using backend.Models.Configuration;
using backend.Repository.Interfaces;
using Microsoft.Extensions.Options;

namespace backend.Background.Classes;

public class N8NIntegration(
  INewsItemRepo newsItemRepo,
  IHttpClientFactory httpClientFactory,
  IOptions<N8NSettings> options,
  ILogger<N8NIntegration> logger) : IN8NIntegration
{
  private readonly N8NSettings settings = options.Value;
  private readonly HttpClient httpClient = httpClientFactory.CreateClient();

  public async Task SendUsersTodayNewsAsync()
  {
    logger.LogInformation("Starting n8n newsletter process");

    List<DateTime> targetDates = [DateTime.UtcNow, DateTime.UtcNow.AddDays(-1)];
    var newsList = await newsItemRepo.GetNewsAsync(targetDates, 0);

    if (newsList.Count == 0)
    {
      logger.LogInformation("No news found for {TargetDates}. Skipping newsletter", string.Join(", ", targetDates));
      return;
    }

    logger.LogInformation("Found {NewsCount} news items. Triggering n8n newsletter workflow", newsList.Count);

    try
    {
      var response = await httpClient.PostAsJsonAsync(settings.NewsLetterWebhookUrl, new
      {
        news = newsList,
        timestamp = DateTime.UtcNow
      });

      if (response.IsSuccessStatusCode)
        logger.LogInformation("Successfully triggered n8n newsletter workflow");

      else
        logger.LogError("Failed to trigger n8n workflow. Status: {StatusCode}", response.StatusCode);
    }
    catch (Exception ex)
    {
      logger.LogError(ex, "Error triggering n8n newsletter workflow");
    }
  }
}