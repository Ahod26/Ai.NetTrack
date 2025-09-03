namespace backend.Models.Dtos;

public class LinkDTO
{
  public string Href { get; private set; }
  public string Rel { get; private set; }
  public string Type { get; private set; }
  public string Method { get; private set; }
  public LinkDTO(string href, string rel, string type, string method = "GET")
  {
    Href = href;
    Rel = rel;
    Type = type;
    Method = method;
  }
}