using System.Collections.Concurrent;
using System.Text.Json;
using backend.MCP.Interfaces;
using backend.Models.Configuration;
using Microsoft.Extensions.Options;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;

namespace backend.MCP.Classes;

public class McpClientService(
  ILogger<McpClientService> logger,
  IOptions<McpSettings> options) : IMcpClientService
{
  private readonly ConcurrentDictionary<string, McpClient> clients = new();
  private readonly ConcurrentDictionary<string, McpClientTool> toolToServerMap = new();
  private McpSettings settings = options.Value;
  private bool _initialized = false;

  public async Task InitializeAsync()
  {
    if (_initialized)
    {
      logger.LogWarning("MCP clients already initialized");
      return;
    }

    logger.LogInformation("Initializing MCP clients using official SDK v0.3.0-preview.4...");

    try
    {
      await InitializeGitHubClient();
      await InitializeDocsClient();

      _initialized = true;
      logger.LogInformation($"Successfully initialized {clients.Count} MCP clients");
    }
    catch (Exception ex)
    {
      logger.LogError(ex, "Failed to initialize MCP clients");
      throw;
    }
  }

  private async Task InitializeGitHubClient()
  {
    const string serverName = "github";

    try
    {
      var token = settings.GitHub.Token;
      if (string.IsNullOrEmpty(token))
      {
        logger.LogWarning("GitHub token not configured, skipping GitHub MCP server");
        return;
      }

      // Use Docker instead of npm
      var clientTransport = new StdioClientTransport(new StdioClientTransportOptions
      {
        Name = "GitHubServer",
        Command = "docker",
        Arguments = [
              "run", "-i", "--rm",
                "-e", "GITHUB_PERSONAL_ACCESS_TOKEN=" + token,
                "ghcr.io/github/github-mcp-server"
          ]
      });

      var client = await McpClient.CreateAsync(clientTransport);

      clients.TryAdd(serverName, client);
      await RegisterToolsForServer(client, serverName);

      logger.LogInformation($"GitHub MCP server '{serverName}' initialized successfully");
    }
    catch (Exception ex)
    {
      logger.LogError(ex, $"Failed to initialize GitHub MCP server '{serverName}'");
    }
  }

  private async Task InitializeDocsClient()
  {
    const string serverName = "docs";

    try
    {
      // Create HttpClient with SSL bypass
      var handler = new HttpClientHandler()
      {
        ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
      };

      var httpClient = new HttpClient(handler);

      var httpTransport = new HttpClientTransport(
        new HttpClientTransportOptions
        {
          Endpoint = new Uri("https://learn.microsoft.com/api/mcp")
        },
        httpClient,
        ownsHttpClient: true
      );

      var client = await McpClient.CreateAsync(httpTransport);

      clients.TryAdd(serverName, client);

      await RegisterToolsForServer(client, serverName);

      logger.LogInformation($"Microsoft Docs MCP server '{serverName}' initialized successfully");
    }
    catch (Exception ex)
    {
      logger.LogError(ex, $"Failed to initialize Microsoft Docs MCP server '{serverName}'");
    }
  }

  private async Task RegisterToolsForServer(McpClient client, string serverName)
  {
    try
    {
      var tools = await client.ListToolsAsync();

      foreach (var tool in tools)
      {
        // Use prefixed tool names to avoid conflicts between servers
        var toolKey = $"{serverName}_{tool.Name}";
        toolToServerMap.TryAdd(toolKey, tool);

        // Also register without prefix for backward compatibility if no conflict exists
        if (!toolToServerMap.ContainsKey(tool.Name))
        {
          toolToServerMap.TryAdd(tool.Name, tool);
        }
      }

      logger.LogDebug($"Registered {tools.Count} tools for server '{serverName}'");
    }
    catch (Exception ex)
    {
      logger.LogError(ex, $"Failed to register tools for server '{serverName}'");
    }
  }

  public IList<McpClientTool> GetAllAvailableToolsAsync()
  {
    return toolToServerMap.Values.Distinct().ToList();
  }

  public async Task<CallToolResult> CallToolAsync(string toolName, Dictionary<string, object?> parameters)
  {
    if (!_initialized)
    {
      throw new InvalidOperationException("MCP clients not initialized. Call InitializeAsync() first.");
    }

    // Find the tool
    if (!toolToServerMap.TryGetValue(toolName, out var tool))
    {
      throw new InvalidOperationException($"Tool '{toolName}' not found in any connected server");
    }

    // Find which server has this tool by checking all registered mappings
    string serverName = clients.Keys.FirstOrDefault(s =>
    {
      // Check if tool exists with server prefix ("github_list_releases")
      if (toolToServerMap.ContainsKey($"{s}_{toolName}"))
        return true;

      // Check if tool already has the server prefix (tool is "github_list_releases" and server is "github")
      if (toolName.StartsWith($"{s}_") && toolToServerMap.ContainsKey(toolName))
        return true;

      return false;
    }) ?? throw new InvalidOperationException($"Could not determine server for tool '{toolName}'");

    if (!clients.TryGetValue(serverName, out var client))
    {
      throw new InvalidOperationException($"Server '{serverName}' not connected");
    }

    try
    {
      // Remove server prefix if present to get the actual tool name
      var actualToolName = toolName.StartsWith($"{serverName}_")
          ? toolName.Substring(serverName.Length + 1)
          : toolName;

      logger.LogDebug($"Calling tool '{actualToolName}' on server '{serverName}' with parameters: {JsonSerializer.Serialize(parameters)}");

      var result = await client.CallToolAsync(actualToolName, parameters);

      logger.LogDebug($"Successfully called tool '{actualToolName}' on server '{serverName}'");
      return result;
    }
    catch (Exception ex)
    {
      logger.LogError(ex, $"Failed to call tool '{toolName}' on server '{serverName}'");
      throw;
    }
  }

}

// GitHub MCP server - https://github.com/github/github-mcp-server
// YouTube MCP server - https://github.com/icraft2170/youtube-data-mcp-server 
// Microsoft Docs MCP server - https://github.com/microsoftdocs/mcp
