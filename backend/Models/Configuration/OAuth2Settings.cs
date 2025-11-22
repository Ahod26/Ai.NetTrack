namespace backend.Models.Configuration;

public class OAuth2Settings
{
  public const string SECTION_NAME = "Authentication:Google";
  public string ClientId { get; set; } = "";
  public string ClientSecret { get; set; } = "";
}