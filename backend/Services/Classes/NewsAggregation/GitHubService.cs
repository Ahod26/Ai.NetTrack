using System.Text.Json;
using backend.Models.Domain;
using backend.Services.Interfaces.LLM;
using backend.Services.Interfaces.NewsAggregation;
using backend.MCP.Interfaces;

namespace backend.Services.Classes.NewsAggregation;

public class GitHubService(
  IOpenAIService openAIService,
  IMcpClientService mcpClientService,
  ILogger<GitHubService> logger
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
        // Get commits from last 24 hours
        var commitsResult = await mcpClientService.CallToolAsync("list_commits", new Dictionary<string, object?>
        {
          ["owner"] = owner,
          ["repo"] = repo,
          ["since"] = DateTime.UtcNow.AddDays(-1).ToString("yyyy-MM-ddTHH:mm:ssZ")
        });

        // Get recent tags
        var releasesResult = await mcpClientService.CallToolAsync("list_releases", new Dictionary<string, object?>
        {
          ["owner"] = owner,
          ["repo"] = repo,
          ["perPage"] = 5 // Recent releases only
        });

        allData.Add(new
        {
          Owner = owner,
          Repo = repo,
          Commits = commitsResult,
          Releases = releasesResult
        });
      }
      catch (Exception ex)
      {
        logger.LogError(ex, $"Failed to get GitHub data for {owner}/{repo}");
      }
    }

    var prompt = $@"
Analyze this GitHub data and return ONLY significant AI/development updates from the LAST 36 HOURS as a JSON array of NewsItem objects.

IMPORTANT TIME FILTERING:
- Only include updates from the last 24 hours (since {DateTime.UtcNow.AddDays(-1):yyyy-MM-dd HH:mm:ss} UTC)
- Commits are already filtered to last 24 hours
- For releases: ONLY include releases published in the last 24 hours, ignore older releases
- Check the published_at, created_at, or date fields to verify timing

CONTENT FILTERING RULES - Only include changes that developers should know about:
- New features and capabilities 
- Breaking changes and API modifications
- Major releases and version updates
- Security fixes and important bug fixes
- Performance improvements
- EXCLUDE: Minor bug fixes, typos, dependency updates, CI/build changes, documentation updates, refactoring

GitHub Data:
{JsonSerializer.Serialize(allData)}

For each significant update from the LAST 24 HOURS, CREATE original content:
- Title: Write a clear, descriptive title explaining what changed
- Content: Write detailed explanation of the change and its impact for developers  
- Summary: Write 1-2 sentence summary of why this matters
- Url: Use the GitHub URL from the data if available
- PublishedDate: Use the actual date from the data
- Id: Always set to 0

Return as JSON array of NewsItem objects. Do NOT include: ImageUrl, SourceType, SourceName

If no significant updates occurred in the last 24 hours, return an empty array.";

    var filteredNews = await openAIService.ProcessGitHubData(prompt);

    if (filteredNews == null) return [];

    // Set the source info
    foreach (var item in filteredNews)
    {
      item.SourceType = NewsSourceType.Github;
      item.SourceName = "GitHub";
    }

    return filteredNews;
  }
}