using System.Text.Json;
using backend.Constants;
using backend.Models.Domain;
using backend.Repository.Interfaces;
using backend.Services.Interfaces.LLM;
using backend.Models.Configuration;
using Microsoft.Extensions.Options;
using backend.Services.Interfaces.Cache;
using backend.Background.Interfaces;

namespace backend.Background.Classes;
public class YouTubeService(
  IOpenAIService openAIService,
  ILogger<YouTubeService> logger,
  INewsItemRepo newsItemRepo,
  INewsCacheService newsCacheService,
  HttpClient httpClient,
  IOptions<McpSettings> options
) : IYouTubeService
{
  private readonly McpSettings settings = options.Value;

  public async Task<List<NewsItem>> GetYouTubeAIUpdatesAsync()
  {
    var channels = new[]
    {
      ("UCsMica-v34Irf9KVTh6xx-g", "Microsoft Developer"), // @MicrosoftDeveloper
      ("UCvtT19MZW8dq5Wwfu6B0oxw", ".NET")                 // @dotnet
    };

    var allData = new List<object>();

    foreach (var (channelId, channelName) in channels)
    {
      try
      {
        var videos = await GetAllChannelVideosLast24HoursAsync(channelId);

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
      await newsCacheService.UpdateNewsGroupsAsync(filteredNews);
      await newsItemRepo.AddItems(filteredNews);
      logger.LogInformation($"Successfully saved {filteredNews.Count} YouTube news items");
    }
    catch (Exception ex)
    {
      logger.LogError(ex, "Failed to save YouTube news items to database");
    }

    return filteredNews;
  }

  private async Task<List<object>> GetAllChannelVideosLast24HoursAsync(string channelId)
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

      var allVideos = new List<object>();

      // 1. Get ALL videos from uploads playlist (most reliable method)
      var uploadsVideos = await GetVideosFromUploadsPlaylistAsync(channelId, publishedAfter, apiKey);
      allVideos.AddRange(uploadsVideos);

      // 2. Also search for live streams that might not be in uploads yet
      var liveVideos = await SearchLiveStreamsAsync(channelId, publishedAfter, apiKey);

      // Add live videos that aren't already in uploads (avoid duplicates)
      var existingVideoIds = allVideos.Select(v => ((dynamic)v).VideoId.ToString()).ToHashSet();
      var newLiveVideos = liveVideos.Where(v => !existingVideoIds.Contains(((dynamic)v).VideoId.ToString()));
      allVideos.AddRange(newLiveVideos);

      logger.LogInformation($"Found {allVideos.Count} total videos in last 24 hours for channel {channelId}");
      return allVideos;
    }
    catch (Exception ex)
    {
      logger.LogError(ex, $"Failed to fetch videos for channel {channelId}");
      return [];
    }
  }

  private async Task<List<object>> GetVideosFromUploadsPlaylistAsync(string channelId, string publishedAfter, string apiKey)
  {
    try
    {
      // First, get the channel's uploads playlist ID
      var channelUrl = $"https://www.googleapis.com/youtube/v3/channels?part=contentDetails&id={channelId}&key={apiKey}";
      var channelResponse = await httpClient.GetStringAsync(channelUrl);
      var channelData = JsonDocument.Parse(channelResponse);

      var uploadsPlaylistId = channelData.RootElement
        .GetProperty("items")[0]
        .GetProperty("contentDetails")
        .GetProperty("relatedPlaylists")
        .GetProperty("uploads")
        .GetString();

      if (string.IsNullOrEmpty(uploadsPlaylistId))
      {
        logger.LogWarning($"Could not find uploads playlist for channel {channelId}");
        return [];
      }

      // Get videos from uploads playlist (ordered by upload date)
      var playlistUrl = $"https://www.googleapis.com/youtube/v3/playlistItems?part=snippet&playlistId={uploadsPlaylistId}&maxResults=50&key={apiKey}";
      var playlistResponse = await httpClient.GetStringAsync(playlistUrl);
      var playlistData = JsonDocument.Parse(playlistResponse);

      var videoIds = new List<string>();
      var publishedAfterDate = DateTime.Parse(publishedAfter);

      // Filter by publish date and collect video IDs
      foreach (var item in playlistData.RootElement.GetProperty("items").EnumerateArray())
      {
        var snippet = item.GetProperty("snippet");
        var publishedAt = DateTime.Parse(snippet.GetProperty("publishedAt").GetString()!);

        // Only include videos from last 24 hours
        if (publishedAt >= publishedAfterDate)
        {
          var videoId = snippet.GetProperty("resourceId").GetProperty("videoId").GetString();
          if (!string.IsNullOrEmpty(videoId))
          {
            videoIds.Add(videoId);
          }
        }
      }

      if (videoIds.Count == 0)
      {
        logger.LogDebug($"No recent videos found in uploads playlist for channel {channelId}");
        return [];
      }

      // Get detailed info for the videos
      return await GetVideoDetails(videoIds, apiKey);
    }
    catch (Exception ex)
    {
      logger.LogError(ex, $"Failed to get uploads playlist videos for channel {channelId}");
      return [];
    }
  }

  private async Task<List<object>> SearchLiveStreamsAsync(string channelId, string publishedAfter, string apiKey)
  {
    try
    {
      // Search specifically for completed live streams
      var searchUrl = $"https://www.googleapis.com/youtube/v3/search?" +
                     $"part=snippet&" +
                     $"channelId={channelId}&" +
                     $"publishedAfter={publishedAfter}&" +
                     $"order=date&" +
                     $"type=video&" +
                     $"eventType=completed&" +
                     $"maxResults=25&" +
                     $"key={apiKey}";

      var searchResponse = await httpClient.GetStringAsync(searchUrl);
      var searchData = JsonDocument.Parse(searchResponse);

      var videoIds = new List<string>();

      foreach (var item in searchData.RootElement.GetProperty("items").EnumerateArray())
      {
        var videoId = item.GetProperty("id").GetProperty("videoId").GetString();
        if (!string.IsNullOrEmpty(videoId))
        {
          videoIds.Add(videoId);
        }
      }

      if (videoIds.Count == 0)
      {
        return [];
      }

      return await GetVideoDetails(videoIds, apiKey);
    }
    catch (Exception ex)
    {
      logger.LogError(ex, $"Failed to search live streams for channel {channelId}");
      return [];
    }
  }

  private async Task<List<object>> GetVideoDetails(List<string> videoIds, string apiKey)
  {
    if (videoIds.Count == 0) return [];

    try
    {
      var videoDetailsUrl = $"https://www.googleapis.com/youtube/v3/videos?" +
                           $"part=snippet,contentDetails,liveStreamingDetails&" +
                           $"id={string.Join(",", videoIds)}&" +
                           $"key={apiKey}";

      var detailsResponse = await httpClient.GetStringAsync(videoDetailsUrl);
      var detailsData = JsonDocument.Parse(detailsResponse);

      var videos = new List<object>();

      foreach (var item in detailsData.RootElement.GetProperty("items").EnumerateArray())
      {
        var snippet = item.GetProperty("snippet");
        var contentDetails = item.GetProperty("contentDetails");
        var duration = contentDetails.GetProperty("duration").GetString();

        // Only include videos longer than 2 minutes
        if (IsVideoLongerThan2Minutes(duration!))
        {
          videos.Add(new
          {
            VideoId = item.GetProperty("id").GetString(),
            Title = snippet.GetProperty("title").GetString(),
            Description = snippet.GetProperty("description").GetString(),
            PublishedAt = snippet.GetProperty("publishedAt").GetString(),
            Duration = duration,
            Thumbnail = snippet.GetProperty("thumbnails").GetProperty("default").GetProperty("url").GetString(),
            LiveBroadcastContent = snippet.GetProperty("liveBroadcastContent").GetString()
          });
        }
      }

      return videos;
    }
    catch (Exception ex)
    {
      logger.LogError(ex, "Failed to get video details");
      return [];
    }
  }

  private static bool IsVideoLongerThan2Minutes(string isoDuration)
  {
    try
    {
      // Parse ISO 8601 duration format (e.g., PT2M30S = 2 minutes 30 seconds)
      var duration = System.Xml.XmlConvert.ToTimeSpan(isoDuration);

      // Only include videos longer than 2 minutes (120 seconds)
      return duration.TotalSeconds > 120;
    }
    catch
    {
      // If parsing fails, assume it's longer than 2 minutes to be safe
      return true;
    }
  }
}