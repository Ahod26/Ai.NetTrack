namespace backend.Constants;

public class ToolTriggers
{
  public static readonly string[] Keywords =
  [      
    // Specific GitHub references
    "github", "repo", "repository"
  ];

  public static bool ShouldUseAllMcpTools(string userMessage)
  {
    var messageLower = userMessage.ToLower();

    // Split message into words
    var words = messageLower.Split([' ', ',', '.', '!', '?', ';', ':', '\n', '\r', '\t'],
                                     StringSplitOptions.RemoveEmptyEntries);

    // Check if any keyword matches as a whole word
    return Keywords.Any(keyword => words.Contains(keyword));
  }
}