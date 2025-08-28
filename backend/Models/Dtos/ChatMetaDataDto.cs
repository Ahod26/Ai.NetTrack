public class ChatMetaDataDto
{
  public Guid Id { get; set; }
  public string Title { get; set; } = "New Chat";
  public DateTime CreatedAt { get; set; }
  public DateTime LastMessageAt { get; set; }
  public int MessageCount { get; set; } = 0;
  public bool IsContextFull { get; set; }
}