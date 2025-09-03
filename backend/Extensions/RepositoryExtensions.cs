using backend.Repository.Interfaces;
using backend.Repository.Classes;

namespace backend.Extensions;

public static class RepositoryExtensions
{
  public static IServiceCollection AddRepositoriesServices(this IServiceCollection services)
  {
    services.AddScoped<IChatRepo, ChatRepo>();
    services.AddScoped<IAuthRepo, AuthRepo>();
    services.AddScoped<IMessagesRepo, MessagesRepo>();

    services.AddSingleton<ILLMCacheRepo, LLMCacheRepo>();
    services.AddSingleton<IChatCacheRepo, ChatCacheRepo>();

    return services;
  }
}