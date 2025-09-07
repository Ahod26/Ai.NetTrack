namespace backend.Models.Domain;
public class NewsItem
{
  public int Id { get; set; }
  public required string Title { get; set; }
  public string? Content { get; set; }
  public string? Url { get; set; }
  public string? ImageUrl { get; set; } 
  public NewsSourceType SourceType { get; set; }
  public string? SourceName { get; set; }
  public DateTime? PublishedDate { get; set; }
  public string? Summary { get; set; }
}
public enum NewsSourceType
{
  Github = 1,
  Rss = 2,
  Youtube = 3,
  Docs = 4
}