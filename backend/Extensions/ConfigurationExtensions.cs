public static class ConfigurationExtensions
{
  public static IServiceCollection AddConfigurationOptions(this IServiceCollection services, IConfiguration configuration)
  {
    services.Configure<OpenAISettings>(configuration.GetSection(OpenAISettings.SECTION_NAME));
    services.Configure<JwtSettings>(configuration.GetSection(JwtSettings.SECTION_NAME));

    return services;
  }
}