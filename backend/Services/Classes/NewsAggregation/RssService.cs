using System.Text.Json;
using System.Xml;
using backend.Models.Domain;
using backend.Repository.Interfaces;
using backend.Services.Interfaces.LLM;
using backend.Services.Interfaces.NewsAggregation;
using backend.Models.Configuration;
using Microsoft.Extensions.Options;
using backend.Constants;
using System.Xml.Linq;
using backend.Services.Interfaces.Cache;
// using System.Net; // removed unused after image extraction removal

namespace backend.Services.Classes.NewsAggregation;

public class RssService(
  IOpenAIService openAIService,
  ILogger<RssService> logger,
  INewsItemRepo newsItemRepo,
  INewsCacheService newsCacheService,
  HttpClient httpClient
) : IRssService
{
  public async Task<List<NewsItem>> GetRSSUpdatesAsync()
  {
    var allData = new List<object>();

    try
    {
      // Get Microsoft .NET DevBlog updates (last 24 hours)
      var dotnetUpdates = await GetMicrosoftDotNetBlogUpdatesAsync();
      if (dotnetUpdates.Count != 0)
      {
        allData.Add(new
        {
          Source = "Microsoft .NET DevBlog",
          Type = "Blog Posts",
          Updates = dotnetUpdates
        });
      }

      if (allData.Count == 0)
      {
        logger.LogInformation("No RSS updates found");
        return [];
      }

      var prompt = PromptConstants.GetRSSNewsPrompt(
          DateTime.UtcNow.AddDays(-1),
          JsonSerializer.Serialize(allData)
      );

      var filteredNews = await openAIService.ProcessNewsData(prompt);

      if (filteredNews == null) return [];

      foreach (var item in filteredNews)
      {
        item.SourceType = NewsSourceType.Rss;
        item.SourceName = "RSS Feeds";
      }

      try
      {
        await newsCacheService.UpdateNewsGroupsAsync(filteredNews);
        await newsItemRepo.AddItems(filteredNews);
        logger.LogInformation($"Successfully saved {filteredNews.Count} RSS news items");
      }
      catch (Exception ex)
      {
        logger.LogError(ex, "Failed to save RSS news items to database");
      }

      return filteredNews;
    }
    catch (Exception ex)
    {
      logger.LogError(ex, "Failed to get RSS updates");
      return [];
    }
  }

  private async Task<List<object>> GetMicrosoftDotNetBlogUpdatesAsync()
  {
    try
    {
      var blogUpdates = new List<object>();
      var yesterday = DateTime.UtcNow.AddDays(-1);

      // Microsoft .NET DevBlog RSS feed
      var rssUrl = "https://devblogs.microsoft.com/dotnet/feed/";

      var response = await httpClient.GetStringAsync(rssUrl);

      // Parse RSS feed using XDocument
      var doc = XDocument.Parse(response);
      var items = doc.Descendants("item");

      foreach (var item in items)
      {
        var pubDateStr = item.Element("pubDate")?.Value;
        if (!DateTime.TryParse(pubDateStr, out var pubDate))
          continue;

        // Check if the item was published in the last 24 hours
        if (pubDate >= yesterday)
        {
          var title = item.Element("title")?.Value ?? "";
          var description = item.Element("description")?.Value ?? "";
          var link = item.Element("link")?.Value ?? "";
          var guid = item.Element("guid")?.Value ?? "";
          var author = item.Element("author")?.Value ?? "";
          var categories = item.Elements("category").Select(c => c.Value).ToArray();

          // Clean HTML from description
          var content = CleanHtmlContent(description);

          // Include all items within last 24 hours; LLM will filter relevance
          blogUpdates.Add(new
          {
            Title = title,
            Content = content,
            Url = link,
            PublishedDate = pubDate,
            Id = guid,
            Author = author,
            Categories = categories
          });
        }
      }

      logger.LogInformation($"Found {blogUpdates.Count} Microsoft .NET DevBlog updates");
      return blogUpdates;
    }
    catch (Exception ex)
    {
      logger.LogError(ex, "Failed to get Microsoft .NET DevBlog updates");
      return [];
    }
  }

  // OpenAI RSS feed support removed 
  private static string CleanHtmlContent(string htmlContent)
  {
    if (string.IsNullOrEmpty(htmlContent))
      return "";

    try
    {
      // Simple HTML cleaning - remove tags but keep content
      var doc = new XmlDocument();
      doc.LoadXml($"<root>{htmlContent}</root>");
      return doc.InnerText;
    }
    catch
    {
      // If XML parsing fails, do basic HTML tag removal
      return System.Text.RegularExpressions.Regex.Replace(htmlContent, "<.*?>", "");
    }
  }
}