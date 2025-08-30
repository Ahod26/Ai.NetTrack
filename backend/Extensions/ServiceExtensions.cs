public static class ServiceExtensions
{
  public static IServiceCollection AddBusinessServices(this IServiceCollection services)
  {
    services.AddScoped<IChatService, ChatService>();
    services.AddScoped<IMessagesService, MessagesServices>();
    services.AddScoped<IAuthService, AuthService>();
    services.AddScoped<ICookieService, CookieService>();

    services.AddSingleton<IOpenAIService, OpenAIService>();
    services.AddSingleton<ITokenService, TokenService>();
    services.AddSingleton<ILLMCacheService, LLMCacheService>();
    services.AddSingleton<ICacheService, CacheService>();
    return services;
  }
}