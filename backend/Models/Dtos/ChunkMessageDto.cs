public class ChunkMessageDto
{
  public required string Content { get; set; } = "";
  public bool IsChunkMessage { get; set; } = true;
}