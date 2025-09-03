namespace backend.Models.Configuration;

public class LLMCacheSettings
{
  public const string SECTION_NAME = "LLMCache";
  public int MaxCacheableMessageCountSemantic { get; set; } = 8;
  public int MaxCacheableMessageCountExact { get; set; } = 2;
  public float SemanticSimilarityThreshold { get; set; } = 0.85f;
  public int BaseCacheLifetimeDays { get; set; } = 21;
  public double CacheLifetimeDecayFactor { get; set; } = 0.7;
}
