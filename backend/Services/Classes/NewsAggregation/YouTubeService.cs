using System.Text.Json;
using backend.Constants;
using backend.Models.Domain;
using backend.Repository.Interfaces;
using backend.Services.Interfaces.LLM;
using backend.Services.Interfaces.NewsAggregation;
using backend.Models.Configuration;
using Microsoft.Extensions.Options;

namespace backend.Services.Classes.NewsAggregation;

public class YouTubeService(
  IOpenAIService openAIService,
  ILogger<YouTubeService> logger,
  INewsItemRepo newsItemRepo,
  HttpClient httpClient,
  IOptions<McpSettings> options
) : IYouTubeService
{
  private readonly McpSettings settings = options.Value;

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
        var videos = await GetChannelVideosLast24HoursAsync(channelId);

        allData.Add(new
        {
          ChannelId = channelId,
          ChannelName = channelName,
          Videos = videos
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

  private async Task<List<object>> GetChannelVideosLast24HoursAsync(string channelId)
  {
    var apiKey = settings.YouTube.Token;

    if (string.IsNullOrEmpty(apiKey))
    {
      logger.LogWarning("YouTube API key not configured");
      return [];
    }

    try
    {
      // Calculate 24 hours ago in RFC 3339 format
      var publishedAfter = DateTime.UtcNow.AddDays(-1).ToString("yyyy-MM-ddTHH:mm:ssZ");

      // Search for videos from the channel published in last 24 hours
      // Using duration=long to exclude shorts (< 4 minutes)
      var searchUrl = $"https://www.googleapis.com/youtube/v3/search?" +
                     $"part=snippet&" +
                     $"channelId={channelId}&" +
                     $"publishedAfter={publishedAfter}&" +
                     $"order=date&" +
                     $"type=video&" +
                     $"videoDuration=long&" +  // Only long videos (>20 minutes) - excludes shorts
                     $"maxResults=50&" +
                     $"key={apiKey}";

      var searchResponse = await httpClient.GetStringAsync(searchUrl);
      var searchData = JsonDocument.Parse(searchResponse);

      var videoIds = new List<string>();
      var videos = new List<object>();

      // Extract video IDs first
      foreach (var item in searchData.RootElement.GetProperty("items").EnumerateArray())
      {
        var videoId = item.GetProperty("id").GetProperty("videoId").GetString();
        if (!string.IsNullOrEmpty(videoId))
        {
          videoIds.Add(videoId);
        }
      }

      // If no videos found with long duration, try medium duration (4-20 minutes)
      if (videoIds.Count == 0)
      {
        var mediumSearchUrl = $"https://www.googleapis.com/youtube/v3/search?" +
                             $"part=snippet&" +
                             $"channelId={channelId}&" +
                             $"publishedAfter={publishedAfter}&" +
                             $"order=date&" +
                             $"type=video&" +
                             $"videoDuration=medium&" +  // Medium videos (4-20 minutes)
                             $"maxResults=50&" +
                             $"key={apiKey}";

        var mediumResponse = await httpClient.GetStringAsync(mediumSearchUrl);
        var mediumData = JsonDocument.Parse(mediumResponse);

        foreach (var item in mediumData.RootElement.GetProperty("items").EnumerateArray())
        {
          var videoId = item.GetProperty("id").GetProperty("videoId").GetString();
          if (!string.IsNullOrEmpty(videoId))
          {
            videoIds.Add(videoId);
          }
        }
      }

      // If we have video IDs, get detailed info including duration to filter out shorts manually
      if (videoIds.Count > 0)
      {
        var videoDetailsUrl = $"https://www.googleapis.com/youtube/v3/videos?" +
                             $"part=snippet,contentDetails&" +
                             $"id={string.Join(",", videoIds)}&" +
                             $"key={apiKey}";

        var detailsResponse = await httpClient.GetStringAsync(videoDetailsUrl);
        var detailsData = JsonDocument.Parse(detailsResponse);

        foreach (var item in detailsData.RootElement.GetProperty("items").EnumerateArray())
        {
          var snippet = item.GetProperty("snippet");
          var contentDetails = item.GetProperty("contentDetails");
          var duration = contentDetails.GetProperty("duration").GetString();

          // Parse duration and filter out shorts (< 60 seconds)
          if (IsLongFormVideo(duration!))
          {
            videos.Add(new
            {
              VideoId = item.GetProperty("id").GetString(),
              Title = snippet.GetProperty("title").GetString(),
              Description = snippet.GetProperty("description").GetString(),
              PublishedAt = snippet.GetProperty("publishedAt").GetString(),
              Duration = duration,
              Thumbnail = snippet.GetProperty("thumbnails").GetProperty("default").GetProperty("url").GetString()
            });
          }
        }
      }

      logger.LogInformation($"Found {videos.Count} long-form videos in last 24 hours for channel {channelId}");
      return videos;
    }
    catch (Exception ex)
    {
      logger.LogError(ex, $"Failed to fetch videos for channel {channelId}");
      return [];
    }
  }

  private static bool IsLongFormVideo(string isoDuration)
  {
    try
    {
      // Parse ISO 8601 duration format (e.g., PT1M30S = 1 minute 30 seconds)
      var duration = System.Xml.XmlConvert.ToTimeSpan(isoDuration);

      // Consider videos longer than 60 seconds as long-form (not shorts)
      return duration.TotalSeconds > 60;
    }
    catch
    {
      // If parsing fails, assume it's long-form to be safe
      return true;
    }
  }
}