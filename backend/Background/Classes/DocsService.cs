using System.Text.Json;
using System.Xml;
using backend.Models.Domain;
using backend.Repository.Interfaces;
using backend.Services.Interfaces.LLM;
using backend.Constants;
using backend.Services.Interfaces.Cache;
using backend.Background.Interfaces;

namespace backend.Background.Classes;

public class DocsService(
    IOpenAIService openAIService,
    ILogger<DocsService> logger,
    INewsItemRepo newsItemRepo,
    INewsCacheService newsCacheService,
    HttpClient httpClient
) : IDocsService
{
  public async Task<List<NewsItem>> GetMicrosoftDocsUpdatesAsync()
  {
    var allData = new List<object>();

    try
    {
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
        await newsCacheService.UpdateNewsGroupsAsync(filteredNews);
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
      Subjects = element.TryGetProperty("subjects", out var subjects) ? subjects.EnumerateArray().Select(s => s.GetString()).ToArray() : [],
      Duration = element.TryGetProperty("duration_in_minutes", out var duration) ? duration.GetInt32() : 0,
      Popularity = element.TryGetProperty("popularity", out var popularity) ? popularity.GetDouble() : 0.0
    };
  }

}