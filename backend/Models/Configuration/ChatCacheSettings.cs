namespace backend.Models.Configuration;

public class ChatCacheSettings
{
  public const string SECTION_NAME = "ChatCache";
  public int CacheDurationHours { get; set; } = 2;
  public TimeSpan CacheDuration => TimeSpan.FromHours(CacheDurationHours);
}