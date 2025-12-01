using backend.Models.Configuration;

namespace backend.Extensions;

public static class ConfigurationExtensions
{
  public static IServiceCollection AddConfigurationOptions(this IServiceCollection services, IConfiguration configuration)
  {
    services.Configure<OpenAISettings>(configuration.GetSection(OpenAISettings.SECTION_NAME));
    services.Configure<JwtSettings>(configuration.GetSection(JwtSettings.SECTION_NAME));
    services.Configure<LLMCacheSettings>(configuration.GetSection(LLMCacheSettings.SECTION_NAME));
    services.Configure<ChatCacheSettings>(configuration.GetSection(ChatCacheSettings.SECTION_NAME));
    services.Configure<StreamingSettings>(configuration.GetSection(StreamingSettings.SECTION_NAME));
    services.Configure<McpSettings>(configuration.GetSection(McpSettings.SECTION_NAME));
    services.Configure<OAuth2Settings>(configuration.GetSection(OAuth2Settings.SECTION_NAME));
    services.Configure<N8NSettings>(configuration.GetSection(N8NSettings.SECTION_NAME));
    services.Configure<NewsCacheSettings>(configuration.GetSection(NewsCacheSettings.SECTION_NAME));

    return services;
  }
}