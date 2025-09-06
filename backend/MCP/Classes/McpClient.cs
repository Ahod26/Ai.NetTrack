using System.Collections.Concurrent;
using System.Text.Json;
using backend.MCP.Interfaces;
using backend.Models.Configuration;
using Microsoft.Extensions.Options;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;

namespace backend.MCP.Classes;

public class McpClientService(ILogger<McpClientService> logger,
  IOptions<McpSettings> options,
  ConcurrentDictionary<string, IMcpClient> clients,
  ConcurrentDictionary<string, string> toolToServerMap) : IMcpClientService, IAsyncDisposable
{
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
      // Initialize all configured servers
      await InitializeGitHubClient();
      await InitializeDocsClient();
      await InitializeYouTubeClient();

      _initialized = true;
      logger.LogInformation($"Successfully initialized {clients.Count} MCP clients");
    }
    catch (Exception ex)
    {
      logger.LogError(ex, "Failed to initialize MCP clients");
      await ShutdownAsync(); // Cleanup on failure
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

      // Set environment variable for the GitHub MCP server
      Environment.SetEnvironmentVariable("GITHUB_TOKEN", token);

      var clientTransport = new StdioClientTransport(new StdioClientTransportOptions
      {
        Name = "GitHubServer",
        Command = "npx",
        Arguments = ["-y", "@github/github-mcp-server"]
      });

      var client = await McpClientFactory.CreateAsync(clientTransport);

      clients.TryAdd(serverName, client);

      // Register tools with server mapping
      await RegisterToolsForServer(client, serverName);

      logger.LogInformation($"GitHub MCP server '{serverName}' initialized successfully");
    }
    catch (Exception ex)
    {
      logger.LogError(ex, $"Failed to initialize GitHub MCP server '{serverName}'");
      // Don't throw - continue with other servers
    }
  }

  private async Task InitializeDocsClient()
  {
    const string serverName = "docs";

    try
    {
      var clientTransport = new StdioClientTransport(new StdioClientTransportOptions
      {
        Name = "DocsServer",
        Command = "npx",
        Arguments = ["-y", "@microsoftdocs/mcp"]
      });

      var client = await McpClientFactory.CreateAsync(clientTransport);

      clients.TryAdd(serverName, client);

      await RegisterToolsForServer(client, serverName);

      logger.LogInformation($"Microsoft Docs MCP server '{serverName}' initialized successfully");
    }
    catch (Exception ex)
    {
      logger.LogError(ex, $"Failed to initialize Docs MCP server '{serverName}'");
    }
  }

  private async Task InitializeYouTubeClient()
  {
    const string serverName = "youtube";

    try
    {
      var youtubeApiKey = settings.YouTube.Token;
      if (string.IsNullOrEmpty(youtubeApiKey))
      {
        logger.LogWarning("YouTube API key not configured, skipping YouTube MCP server");
        return;
      }

      // Set environment variable for the YouTube MCP server
      Environment.SetEnvironmentVariable("YOUTUBE_API_KEY", youtubeApiKey);

      var clientTransport = new StdioClientTransport(new StdioClientTransportOptions
      {
        Name = "YouTubeServer",
        Command = "npx",
        Arguments = ["-y", "youtube-data-mcp-server"]
      });

      var client = await McpClientFactory.CreateAsync(clientTransport);

      clients.TryAdd(serverName, client);

      await RegisterToolsForServer(client, serverName);

      logger.LogInformation($"YouTube MCP server '{serverName}' initialized successfully");
    }
    catch (Exception ex)
    {
      logger.LogError(ex, $"Failed to initialize YouTube MCP server '{serverName}'");
    }
  }

  private async Task RegisterToolsForServer(IMcpClient client, string serverName)
  {
    try
    {
      var tools = await client.ListToolsAsync();

      foreach (var tool in tools)
      {
        // Use prefixed tool names to avoid conflicts between servers
        var toolKey = $"{serverName}_{tool.Name}";
        toolToServerMap.TryAdd(toolKey, serverName);

        // Also register without prefix for backward compatibility if no conflict exists
        if (!toolToServerMap.ContainsKey(tool.Name))
        {
          toolToServerMap.TryAdd(tool.Name, serverName);
        }
      }

      logger.LogDebug($"Registered {tools.Count} tools for server '{serverName}'");
    }
    catch (Exception ex)
    {
      logger.LogError(ex, $"Failed to register tools for server '{serverName}'");
    }
  }

  public async Task<IList<McpClientTool>> GetAllAvailableToolsAsync()
  {
    var allTools = new List<McpClientTool>();

    foreach (var kvp in clients)
    {
      try
      {
        var tools = await kvp.Value.ListToolsAsync();
        allTools.AddRange(tools);
      }
      catch (Exception ex)
      {
        logger.LogError(ex, $"Failed to get tools from server '{kvp.Key}'");
      }
    }

    logger.LogDebug($"Retrieved {allTools.Count} tools across {clients.Count} servers");
    return allTools;
  }

  public async Task<CallToolResult> CallToolAsync(string toolName, Dictionary<string, object?> parameters)
  {
    if (!_initialized)
    {
      throw new InvalidOperationException("MCP clients not initialized. Call InitializeAsync() first.");
    }

    // Find the server for this tool
    if (!toolToServerMap.TryGetValue(toolName, out var serverName))
    {
      throw new InvalidOperationException($"Tool '{toolName}' not found in any connected server");
    }

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

  public bool IsConnected(string serverName)
  {
    return clients.ContainsKey(serverName);
  }

  public async Task<List<string>> GetConnectedServersAsync()
  {
    var connectedServers = new List<string>();

    foreach (var kvp in clients)
    {
      try
      {
        // Try to ping the server to verify it's actually responsive
        await kvp.Value.ListToolsAsync();
        connectedServers.Add(kvp.Key);
      }
      catch (Exception ex)
      {
        logger.LogWarning(ex, $"Server '{kvp.Key}' appears disconnected");
      }
    }

    return connectedServers;
  }

  public async Task ShutdownAsync()
  {
    logger.LogInformation("Shutting down MCP clients...");

    var shutdownTasks = new List<Task>();

    // Dispose clients
    foreach (var kvp in clients)
    {
      shutdownTasks.Add(Task.Run(async () =>
      {
        try
        {
          await kvp.Value.DisposeAsync();
          logger.LogDebug($"Disposed client for server '{kvp.Key}'");
        }
        catch (Exception ex)
        {
          logger.LogError(ex, $"Error disposing client for server '{kvp.Key}'");
        }
      }));
    }

    await Task.WhenAll(shutdownTasks);

    clients.Clear();
    toolToServerMap.Clear();
    _initialized = false;

    logger.LogInformation("All MCP clients shut down successfully");
  }

  public async ValueTask DisposeAsync()
  {
    await ShutdownAsync();
    GC.SuppressFinalize(this);
  }
}