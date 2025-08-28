public class JwtSettings
{
  public const string SECTION_NAME = "JwtSettings";
  public string SecretKey { get; set; } = "";
  public string Issuer { get; set; } = "";
  public string Audience { get; set; } = "";
  public int ExpirationInMinutes { get; set; }
}