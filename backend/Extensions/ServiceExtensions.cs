public static class ServiceExtensions
{
  public static IServiceCollection AddBusinessServices(this IServiceCollection services)
  {
    services.AddScoped<IChatService, ChatService>();
    services.AddScoped<IAuthService, AuthService>();
    services.AddScoped<ICookieService, CookieService>();
    services.AddScoped<ITokenService, TokenService>();
    services.AddScoped<IOpenAIService, OpenAIService>();
    return services;
  }
}