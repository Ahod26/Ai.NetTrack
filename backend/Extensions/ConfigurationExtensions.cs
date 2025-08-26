public static class ConfigurationExtensions
{
  public static IServiceCollection AddConfigurationOptions(this IServiceCollection services, IConfiguration configuration)
  {
    services.Configure<OpenAISettings>(configuration.GetSection("OpenAI"));
    services.Configure<JwtSettings>(configuration.GetSection("JwtSettings"));

    return services;
  }
}