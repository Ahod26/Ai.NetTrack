namespace backend.Configuration;

public class OpenAISettings
{
  public const string SECTION_NAME = "OpenAI";
  public string ApiKey { get; set; } = "";
  public string Model { get; set; } = "gpt-4.1";
  public int MaxToken { get; set; } = 4096;
  public double Temperature { get; set; } = 0.7;
  public float TitleGenerationTemperature { get; set; } = 0.3f;
}