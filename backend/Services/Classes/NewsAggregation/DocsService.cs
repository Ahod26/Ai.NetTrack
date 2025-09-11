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

namespace backend.Services.Classes.NewsAggregation;

public class DocsService(
    IOpenAIService openAIService,
    ILogger<DocsService> logger,
    INewsItemRepo newsItemRepo,
    HttpClient httpClient,
    IOptions<McpSettings> options
) : IDocsService
{
  private readonly McpSettings settings = options.Value;

  public async Task<List<NewsItem>> GetMicrosoftDocsUpdatesAsync()
  {
    var allData = new List<object>();

    try
    {
      // 1. Get recent updates from Microsoft Learn Catalog API (all items in last 24 hours)
      var learnUpdates = await GetMicrosoftLearnUpdatesAsync();
      if (learnUpdates.Count != 0)
      {
        allData.Add(new
        {
          Source = "Microsoft Learn Catalog",
          Type = "Learning Content",
          Updates = learnUpdates
        });
      }

      // 2. Get Microsoft Graph changelog updates via RSS (all items in last 24 hours)
      var graphUpdates = await GetMicrosoftGraphUpdatesAsync();
      if (graphUpdates.Count != 0)
      {
        allData.Add(new
        {
          Source = "Microsoft Graph Changelog",
          Type = "API Updates",
          Updates = graphUpdates
        });
      }

      if (allData.Count == 0)
      {
        logger.LogInformation("No documentation updates found");
        return [];
      }

      var prompt = PromptConstants.GetDocsNewsPrompt(
          DateTime.UtcNow.AddDays(-1),
          JsonSerializer.Serialize(allData)
      );

      var filteredNews = await openAIService.ProcessNewsData(prompt);

      if (filteredNews == null) return [];

      foreach (var item in filteredNews)
      {
        item.SourceType = NewsSourceType.Docs;
        item.SourceName = "Microsoft Docs";
      }

      try
      {
        await newsItemRepo.AddItems(filteredNews);
        logger.LogInformation($"Successfully saved {filteredNews.Count} documentation news items");
      }
      catch (Exception ex)
      {
        logger.LogError(ex, "Failed to save documentation news items to database");
      }

      return filteredNews;
    }
    catch (Exception ex)
    {
      logger.LogError(ex, "Failed to get Microsoft documentation updates");
      return [];
    }
  }

  private async Task<List<object>> GetMicrosoftLearnUpdatesAsync()
  {
    try
    {
      var yesterday = DateTime.UtcNow.AddDays(-1);
      var lastModifiedFilter = yesterday.ToString("yyyy-MM-dd");

      var recentUpdates = new List<object>();

      // Single query for all modules and learning paths modified in the last 24 hours
      var url = "https://learn.microsoft.com/api/catalog?" +
                $"locale=en-us&" +
                $"last_modified=gte {Uri.EscapeDataString(lastModifiedFilter)}&" +
                "type=modules,learningPaths";

      try
      {
        var response = await httpClient.GetStringAsync(url);
        var catalogData = JsonDocument.Parse(response);

        if (catalogData.RootElement.TryGetProperty("modules", out var modules))
        {
          foreach (var module in modules.EnumerateArray())
          {
            var lastModified = DateTime.Parse(module.GetProperty("last_modified").GetString()!);
            if (lastModified >= yesterday)
            {
              recentUpdates.Add(CreateUpdateObject(module, "module"));
            }
          }
        }

        if (catalogData.RootElement.TryGetProperty("learningPaths", out var paths))
        {
          foreach (var path in paths.EnumerateArray())
          {
            var lastModified = DateTime.Parse(path.GetProperty("last_modified").GetString()!);
            if (lastModified >= yesterday)
            {
              recentUpdates.Add(CreateUpdateObject(path, "learningPath"));
            }
          }
        }
      }
      catch (Exception ex)
      {
        logger.LogWarning(ex, "Failed to get Learn Catalog updates");
      }

      logger.LogInformation($"Found {recentUpdates.Count} recent updates from Microsoft Learn Catalog");
      return recentUpdates;
    }
    catch (Exception ex)
    {
      logger.LogError(ex, "Failed to get Microsoft Learn Catalog updates");
      return [];
    }
  }

  private static object CreateUpdateObject(JsonElement element, string type)
  {
    return new
    {
      Type = type,
      Uid = element.GetProperty("uid").GetString(),
      Title = element.GetProperty("title").GetString(),
      Summary = element.GetProperty("summary").GetString(),
      Url = element.GetProperty("url").GetString(),
      LastModified = element.GetProperty("last_modified").GetString(),
      Products = element.GetProperty("products").EnumerateArray().Select(p => p.GetString()).ToArray(),
      Levels = element.GetProperty("levels").EnumerateArray().Select(l => l.GetString()).ToArray(),
      Roles = element.GetProperty("roles").EnumerateArray().Select(r => r.GetString()).ToArray(),
      Subjects = element.TryGetProperty("subjects", out var subjects)
            ? subjects.EnumerateArray().Select(s => s.GetString()).ToArray()
            : Array.Empty<string>(),
      Duration = element.TryGetProperty("duration_in_minutes", out var duration)
            ? duration.GetInt32()
            : 0,
      Popularity = element.TryGetProperty("popularity", out var popularity)
            ? popularity.GetDouble()
            : 0.0
    };
  }

  private async Task<List<object>> GetMicrosoftGraphUpdatesAsync()
  {
    try
    {
      var graphUpdates = new List<object>();
      var yesterday = DateTime.UtcNow.AddDays(-1);

      // Microsoft Graph RSS changelog feed
      var rssUrl = "https://developer.microsoft.com/en-us/graph/changelog/rss";

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

          // Clean HTML from description
          var content = CleanHtmlContent(description);

          // Include all items within last 24 hours; LLM will filter relevance
          graphUpdates.Add(new
          {
            Title = title,
            Content = content,
            Url = link,
            PublishedDate = pubDate,
            Id = guid
          });
        }
      }

      logger.LogInformation($"Found {graphUpdates.Count} relevant Microsoft Graph updates");
      return graphUpdates;
    }
    catch (Exception ex)
    {
      logger.LogError(ex, "Failed to get Microsoft Graph changelog updates");
      return [];
    }
  }

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