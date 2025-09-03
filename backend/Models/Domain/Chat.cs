namespace backend.Models.Domain;

public class Chat
{
  public Guid Id { get; set; }
  public required string UserId { get; set; } //FK
  public string Title { get; set; } = "New Chat";
  public DateTime CreatedAt { get; set; }
  public DateTime LastMessageAt { get; set; }
  public int MessageCount { get; set; } = 0;
  public bool IsContextFull { get; set; } = false;

  //navigation properties
  public ApiUser User { get; set; } = null!;
  public List<ChatMessage> Messages { get; set; } = new();
}