namespace backend.Configuration;

public class StreamingSettings
{
  public const string SECTION_NAME = "Streaming";
  public int ChunkSize { get; set; } = 3;
  public int DelayMs { get; set; } = 50;
  public TimeSpan Delay => TimeSpan.FromMilliseconds(DelayMs);
}
