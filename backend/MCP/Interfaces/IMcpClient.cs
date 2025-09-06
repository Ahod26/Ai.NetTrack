using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;

namespace backend.MCP.Interfaces;

public interface IMcpClientService
{
  Task InitializeAsync();
  Task<IList<McpClientTool>> GetAllAvailableToolsAsync();
  Task<CallToolResult> CallToolAsync(string toolName, Dictionary<string, object?> parameters);
  Task ShutdownAsync();
  bool IsConnected(string serverName);
  Task<List<string>> GetConnectedServersAsync();
}