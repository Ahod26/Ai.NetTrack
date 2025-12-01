namespace backend.Models.Configuration;

public class NewsCacheSettings
{
  public const string SECTION_NAME = "NewsCache";
  public int CacheDurationDays { get; set; } = 15;
  public TimeSpan CacheDuration => TimeSpan.FromDays(CacheDurationDays);
}