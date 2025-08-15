public class FullMessageDto
{
  public Guid Id { get; set; }
  public required string Content { get; set; } = "";
  public required MessageType Type { get; set; }
  public required DateTime CreatedAt { get; set; }
  public bool IsChunkMessage { get; set; } = false;
}