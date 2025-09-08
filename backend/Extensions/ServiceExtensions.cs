using backend.Services.Interfaces.NewsAggregation;
using backend.Services.Classes.NewsAggregation;
using backend.Services.Interfaces.Cache;
using backend.Services.Classes.Cache;
using backend.Services.Interfaces.Chat;
using backend.Services.Classes.ChatService;
using backend.Services.Interfaces.Auth;
using backend.Services.Classes.Auth;
using backend.Services.Interfaces.LLM;
using backend.Services.Classes.LLM;
using ModelContextProtocol.Client;
using backend.MCP.Classes;
using backend.MCP.Interfaces;

namespace backend.Extensions;

public static class ServiceExtensions
{
  public static IServiceCollection AddBusinessServices(this IServiceCollection services)
  {
    services.AddScoped<IChatService, ChatService>();
    services.AddScoped<IMessagesService, MessagesServices>();
    services.AddScoped<IAuthService, AuthService>();
    services.AddScoped<ICookieService, CookieService>();
    services.AddScoped<INewsCollectorService, NewsCollectorService>();
    services.AddScoped<IGitHubService, GitHubService>();
    services.AddScoped<IRssService, RssService>();
    services.AddScoped<IYouTubeService, YouTubeService>();
    services.AddScoped<IDocsService, DocsService>();
    services.AddScoped<IMcpClientService, McpClientService>();

    services.AddSingleton<IOpenAIService, OpenAIService>();
    services.AddSingleton<ITokenService, TokenService>();
    services.AddSingleton<ILLMCacheService, LLMCacheService>();
    services.AddSingleton<ICacheService, CacheService>();
    return services;
  }
}