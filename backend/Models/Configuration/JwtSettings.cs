public class JwtSettings
{
  public const string SECTION_NAME = "JwtSettings";
  public string SecretKey { get; set; } = "";
  public string Issuer { get; set; } = "AiNetTrackAPI";
  public string Audience { get; set; } = "AiNetTrackAPI";
  public int ExpirationInMinutes { get; set; } = 10000;
}