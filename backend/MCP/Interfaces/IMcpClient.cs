using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;

namespace backend.MCP.Interfaces;

public interface IMcpClientService
{
  Task InitializeAsync();
  IList<McpClientTool> GetAllAvailableToolsAsync();
  List<McpClientTool> GetEssentialTools();
  Task<CallToolResult> CallToolAsync(string toolName, Dictionary<string, object?> parameters);
}