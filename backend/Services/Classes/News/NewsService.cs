using System.Text.Json;
using System.Xml;
using System.Xml.Linq;
using backend.MCP.Interfaces;
using backend.Models.Domain;
using backend.Repository.Interfaces;
using backend.Services.Interfaces.Cache;
using backend.Services.Interfaces.News;

namespace backend.Services.Classes.News;

public class NewsService(
  INewsItemRepo newsRepo,
  INewsCacheService newsCacheService,
  ILogger<NewsService> logger
) : INewsService
{
  public async Task<List<NewsItem>> GetNewsItems(DateTime targetDates, int newsType)
  {
    try
    {
      var newsList = await newsCacheService.GetNewsAsync(targetDates, newsType);
      if (newsList.Count != 0)
      {
        logger.LogWarning("cache hit");
        return newsList;
      }
      var newsListFromDB = await newsRepo.GetNewsAsync(targetDates, newsType);
      await newsCacheService.UpdateNewsGroupsAsync(newsListFromDB);
      logger.LogWarning("DB hit");
      return newsListFromDB;
    }
    catch (Exception ex)
    {
      logger.LogError(ex, "Failed to get news items");
      throw;
    }
  }

  public async Task<string?> GetContentForRelatedNews(string relatedNewsURL)
  {
    try
    {
      // Check if it's a YouTube URL
      if (relatedNewsURL.Contains("youtube.com"))
      {
        var videoId = ExtractYouTubeVideoId(relatedNewsURL);
        if (string.IsNullOrEmpty(videoId))
        {
          logger.LogWarning($"Could not extract video ID from URL: {relatedNewsURL}");
          return null;
        }

        return await GetYouTubeTranscriptAsync(videoId);
      }
      // Check if it's a Microsoft .NET DevBlog URL
      else if (relatedNewsURL.Contains("devblogs.microsoft.com/dotnet"))
      {
        return await GetBlogContentAsync(relatedNewsURL);
      }
      else if (relatedNewsURL.Contains("github.com"))
      {
        return "Github content accessible through MCP server";
      }
      else
      {
        logger.LogWarning($"Unsupported URL type: {relatedNewsURL}");
        return null;
      }
    }
    catch (Exception ex)
    {
      logger.LogError(ex, $"Failed to get content for URL: {relatedNewsURL}");
      return null;
    }
  }

  private static string? ExtractYouTubeVideoId(string url)
  {
    try
    {
      var uri = new Uri(url);
      var query = System.Web.HttpUtility.ParseQueryString(uri.Query);
      return query["v"];
    }
    catch
    {
      return null;
    }
  }

  private async Task<string?> GetYouTubeTranscriptAsync(string videoId)
  {
    try
    {
      var startInfo = new System.Diagnostics.ProcessStartInfo
      {
        FileName = "/Users/arbelhodesman/.local/bin/youtube_transcript_api",
        Arguments = $"{videoId} --format json",
        RedirectStandardOutput = true,
        RedirectStandardError = true,
        UseShellExecute = false,
        CreateNoWindow = true
      };

      using var process = System.Diagnostics.Process.Start(startInfo);
      if (process == null)
      {
        logger.LogWarning($"Failed to start transcript process for video: {videoId}");
        return null;
      }

      var output = await process.StandardOutput.ReadToEndAsync();
      var error = await process.StandardError.ReadToEndAsync();
      await process.WaitForExitAsync();

      if (process.ExitCode != 0)
      {
        logger.LogWarning($"Transcript fetch failed for video {videoId}: {error}");
        return null;
      }

      if (string.IsNullOrWhiteSpace(output))
      {
        logger.LogWarning($"Empty transcript returned for video: {videoId}");
        return null;
      }

      // Parse JSON - it's a nested array [[{...}, {...}]]
      var transcriptData = JsonDocument.Parse(output);
      var transcriptText = new System.Text.StringBuilder();

      // Get the first (and only) array element
      var transcriptArray = transcriptData.RootElement[0];

      foreach (var segment in transcriptArray.EnumerateArray())
      {
        var text = segment.GetProperty("text").GetString();
        if (!string.IsNullOrWhiteSpace(text))
        {
          transcriptText.Append(text).Append(" ");
        }
      }

      var finalTranscript = transcriptText.ToString().Trim();
      logger.LogInformation($"Successfully fetched transcript for video {videoId} ({finalTranscript.Length} chars)");

      return finalTranscript;
    }
    catch (Exception ex)
    {
      logger.LogError(ex, $"Failed to get transcript for video: {videoId}");
      return null;
    }
  }

  private async Task<string?> GetBlogContentAsync(string blogUrl)
  {
    try
    {
      var httpClient = new HttpClient();

      // Parse the RSS feed to find the matching blog post
      var rssUrl = "https://devblogs.microsoft.com/dotnet/feed/";
      var response = await httpClient.GetStringAsync(rssUrl);

      var doc = XDocument.Parse(response);
      var items = doc.Descendants("item");

      foreach (var item in items)
      {
        var link = item.Element("link")?.Value ?? "";

        // Match the URL
        if (link.Equals(blogUrl, StringComparison.OrdinalIgnoreCase))
        {
          var title = item.Element("title")?.Value ?? "";
          var description = item.Element("description")?.Value ?? "";
          var content = CleanHtmlContent(description);

          logger.LogInformation($"Found blog content for: {title}");
          return $"Title: {title}\n\nContent:\n{content}";
        }
      }

      logger.LogWarning($"Blog post not found in RSS feed: {blogUrl}");
      return null;
    }
    catch (Exception ex)
    {
      logger.LogError(ex, $"Failed to get blog content for URL: {blogUrl}");
      return null;
    }
  }

  private static string CleanHtmlContent(string htmlContent)
  {
    if (string.IsNullOrEmpty(htmlContent))
      return "";

    try
    {
      var doc = new XmlDocument();
      doc.LoadXml($"<root>{htmlContent}</root>");
      return doc.InnerText;
    }
    catch
    {
      return System.Text.RegularExpressions.Regex.Replace(htmlContent, "<.*?>", "");
    }
  }
}