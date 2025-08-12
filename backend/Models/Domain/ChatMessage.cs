public class ChatMessage
{
  public Guid Id { get; set; }
  public Guid ChatId { get; set; } //FK
  public string Content { get; set; } = "";
  public MessageType Type { get; set; }
  public DateTime CreatedAt { get; set; }
  public int TokenCount { get; set; } = 0;

  //navigation properties
  public Chat Chat { get; set; } = null!;
}

public enum MessageType
{
  User,
  Assistant
}