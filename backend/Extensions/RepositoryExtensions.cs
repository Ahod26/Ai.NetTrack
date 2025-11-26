using backend.Repository.Interfaces;
using backend.Repository.Classes;
using AutoMapper;

namespace backend.Extensions;

public static class RepositoryExtensions
{
  public static IServiceCollection AddRepositoriesServices(this IServiceCollection services)
  {
    services.AddScoped<IChatRepo, ChatRepo>();
    services.AddScoped<IAuthRepo, AuthRepo>();
    services.AddScoped<IMessagesRepo, MessagesRepo>();
    services.AddScoped<INewsItemRepo, NewsItemsRepo>();
    services.AddScoped<IProfileRepo, ProfileRepo>();

    services.AddSingleton<ILLMCacheRepo, LLMCacheRepo>();
    services.AddSingleton<IChatCacheRepo, ChatCacheRepo>();
    services.AddSingleton<INewsCacheRepo, NewsCacheRepo>();
    return services;
  }
}