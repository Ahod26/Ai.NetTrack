namespace backend.Constants;

public class ToolTriggers
{
  public static readonly string[] Keywords =
  [
      // AI Development Topics (your app's focus)
      "mcp", "model context protocol",
      "openai", "chatgpt", "gpt", "claude", "anthropic",
      "llm", "large language model", "language model",
      "embeddings", "vector", "semantic",
      "rag", "retrieval augmented generation","chatbot",
      "prompt", "prompting", "system prompt",
      "tool calling", "tools","ai sdk", "openai sdk",
      "anthropic sdk", "ai", "sdk", 
      
      // Temporal keywords (latest/current info)
      "latest", "newest", "recent", "current", "new",
      "updated", "latest release", "new version",
      "what's new", "recent changes", "new features", "most accurate",
      
      // Specific GitHub references
      "github", "repo", "repository",
      
      // Documentation for AI-related topics
      "docs", "documentation"
  ];

  public static bool ShouldUseMcpTools(string userMessage)
  {
    var messageLower = userMessage.ToLower();

    // Split message into words
    var words = messageLower.Split([' ', ',', '.', '!', '?', ';', ':', '\n', '\r', '\t'],
                                     StringSplitOptions.RemoveEmptyEntries);

    // Check if any keyword matches as a whole word
    return Keywords.Any(keyword => words.Contains(keyword));
  }
}