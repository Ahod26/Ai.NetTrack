using System.Text.Json;
using backend.Models.Domain;
using backend.Services.Interfaces.LLM;
using backend.Services.Interfaces.NewsAggregation;
using backend.MCP.Interfaces;
using backend.Repository.Interfaces;
using backend.Constants;
using backend.Services.Interfaces.Cache;

namespace backend.Services.Classes.NewsAggregation;

public class GitHubService(
  IOpenAIService openAIService,
  IMcpClientService mcpClientService,
  ILogger<GitHubService> logger,
  INewsItemRepo newsItemRepo,
  INewsCacheService newsCacheService
) : IGitHubService
{
  public async Task<List<NewsItem>> GetGitHubAIUpdatesAsync()
  {
    var repos = new[]
    {
        ("microsoft", "semantic-kernel"),
        ("openai", "openai-dotnet"),
        ("dotnet", "extensions"),
        ("modelcontextprotocol", "csharp-sdk"),
        ("Azure", "azure-sdk-for-net")
    };

    var allData = new List<object>();

    foreach (var (owner, repo) in repos)
    {
      try
      {
        // Only get recent releases 
        var releasesResult = await mcpClientService.CallToolAsync("list_releases", new Dictionary<string, object?>
        {
          ["owner"] = owner,
          ["repo"] = repo,
          ["perPage"] = 5 // Last 5 releases
        });

        allData.Add(new
        {
          Owner = owner,
          Repo = repo,
          Releases = releasesResult
        });
      }
      catch (Exception ex)
      {
        logger.LogError(ex, $"Failed to get GitHub data for {owner}/{repo}");
      }
    }

    var prompt = PromptConstants.GetGitHubNewsPrompt(DateTime.UtcNow.AddDays(-1), JsonSerializer.Serialize(allData));

    var filteredNews = await openAIService.ProcessNewsData(prompt);

    if (filteredNews == null) return [];

    // Set the source info
    foreach (var item in filteredNews)
    {
      item.SourceType = NewsSourceType.Github;
      item.SourceName = "GitHub";
    }

    try
    {
      await newsCacheService.UpdateNewsGroupsAsync(filteredNews);
      await newsItemRepo.AddItems(filteredNews);
      logger.LogInformation($"Successfully saved {filteredNews.Count} GitHub news items");
    }
    catch (Exception ex)
    {
      logger.LogError(ex, "Failed to save GitHub news items to database");
    }

    return filteredNews;
  }
}