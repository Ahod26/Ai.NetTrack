using backend.Background.Interfaces;
using backend.Models.Configuration;
using backend.Models.Domain;
using backend.Models.Dtos;
using backend.Repository.Interfaces;
using backend.Services.Interfaces;
using Microsoft.Extensions.Options;

namespace backend.Background.Classes;

public class N8NIntegration(
  INewsItemRepo newsItemRepo,
  IEmailListCacheService emailListCacheService,
  IOptions<N8NSettings> options,
  ILogger<N8NIntegration> logger) : IN8NIntegration
{
  private readonly N8NSettings settings = options.Value;
  private readonly HttpClient httpClient = CreateHttpClientWithSslBypass();

  // SSL bypass, something with mac, if i will make it to production i need to remove it
  private static HttpClient CreateHttpClientWithSslBypass()
  {
    var handler = new HttpClientHandler()
    {
      ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
    };
    return new HttpClient(handler);
  }

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

    logger.LogInformation("Found {NewsCount} news items.", newsList.Count);

    var emailList = await emailListCacheService.GetNewsletterRecipients();

    if (emailList.Count == 0)
    {
      logger.LogInformation("No recipient found");
      return;
    }

    logger.LogInformation("Found {UserCount} recipients. Triggering n8n newsletter workflow", emailList.Count);

    try
    {
      var response = await httpClient.PostAsJsonAsync(settings.NewsletterWebhookUrl, new
      {
        news = newsList,
        recipients = emailList,
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