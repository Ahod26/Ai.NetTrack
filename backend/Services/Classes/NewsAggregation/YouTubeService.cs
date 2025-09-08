using System.Text.Json;
using backend.Constants;
using backend.MCP.Interfaces;
using backend.Models.Domain;
using backend.Repository.Interfaces;
using backend.Services.Interfaces.LLM;
using backend.Services.Interfaces.NewsAggregation;

namespace backend.Services.Classes.NewsAggregation;

public class YouTubeService(
  IOpenAIService openAIService,
  IMcpClientService mcpClientService,
  ILogger<YouTubeService> logger,
  INewsItemRepo newsItemRepo
) : IYouTubeService
{
  public async Task<List<NewsItem>> GetYouTubeAIUpdatesAsync()
  {
    var channels = new[]
    {
      ("UCWv7vMbMWH4-V0ZXdmDpPBA", "Microsoft Developer"), // @MicrosoftDeveloper
      ("UCvtT19MZW8dq5Wwfu6B0oxw", ".NET")                 // @dotnet
    };

    var allData = new List<object>();

    foreach (var (channelId, channelName) in channels)
    {
      try
      {
        // Get recent videos from channel (last 24 hours worth)
        var channelVideosResult = await mcpClientService.CallToolAsync("getChannelVideos", new Dictionary<string, object?>
        {
          ["channelId"] = channelId,
          ["maxResults"] = 10 // Get recent videos
        });

        allData.Add(new
        {
          ChannelId = channelId,
          ChannelName = channelName,
          Videos = channelVideosResult
        });
      }
      catch (Exception ex)
      {
        logger.LogError(ex, $"Failed to get YouTube data for channel {channelName}");
      }
    }

    var prompt = PromptConstants.GetYouTubeNewsPrompt(
        DateTime.UtcNow.AddDays(-1),
        JsonSerializer.Serialize(allData)
    );

    var filteredNews = await openAIService.ProcessNewsData(prompt);

    if (filteredNews == null) return [];

    foreach (var item in filteredNews)
    {
      item.SourceType = NewsSourceType.Youtube;
      item.SourceName = "YouTube";
    }

    try
    {
      await newsItemRepo.AddItems(filteredNews);
      logger.LogInformation($"Successfully saved {filteredNews.Count} YouTube news items");
    }
    catch (Exception ex)
    {
      logger.LogError(ex, "Failed to save YouTube news items to database");
    }

    return filteredNews;
  }
}