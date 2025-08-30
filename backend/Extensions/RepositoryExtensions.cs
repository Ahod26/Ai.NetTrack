public static class RepositoryExtensions
{
  public static IServiceCollection AddRepositoriesServices(this IServiceCollection services)
  {
    services.AddScoped<IChatRepo, ChatRepo>();
    services.AddScoped<IAuthRepo, AuthRepo>();
    services.AddScoped<IMessagesRepo, MessagesRepo>();

    services.AddSingleton<ILLMCacheRepo, LLMCacheRepo>();
    
    return services;
  }
}