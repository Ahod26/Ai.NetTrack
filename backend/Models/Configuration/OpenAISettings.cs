public class OpenAISettings
{
  public const string SECTION_NAME = "OpenAI";
  public string ApiKey { get; set; } = "";
  public string Model { get; set; } = "";
  public int MaxToken { get; set; }
  public double Temperature { get; set; }
}