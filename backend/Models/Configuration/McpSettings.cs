namespace backend.Models.Configuration;

public class McpSettings
{
  public const string SECTION_NAME = "MCP";
  public GitHubSettings GitHub { get; set; } = new();
  public YouTubeSetting YouTube { get; set; } = new();
}

public class GitHubSettings
{
  public string Token { get; set; } = "";
}

public class YouTubeSetting
{
  public string Token { get; set; } = "";
}