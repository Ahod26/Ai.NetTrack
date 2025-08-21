using OpenAI;
using OpenAI.Embeddings;
using StackExchange.Redis;

public static class ExternalServicesExtensions
{
  public static IServiceCollection AddExternalServices(this IServiceCollection services, IConfiguration configuration)
  {
    services.AddSingleton(serviceProvider =>
    {
      var apiKey = configuration["OpenAI:ApiKey"] ?? Environment.GetEnvironmentVariable("OPENAI_API_KEY");
      return new OpenAIClient(apiKey);
    });

    services.AddSingleton(serviceProvider =>
    {
      var openAIClient = serviceProvider.GetRequiredService<OpenAIClient>();
      var model = configuration["OpenAI:Model"] ?? "gpt-4o-mini";
      return openAIClient.GetChatClient(model);
    });

    services.AddSingleton(provider => 
    new EmbeddingClient("text-embedding-3-small", configuration["OpenAI:ApiKey"]));

    services.AddStackExchangeRedisCache(options =>
    {
      options.Configuration = configuration["Redis:ConnectionString"];
    });

    services.AddSingleton<IConnectionMultiplexer>(provider =>
          ConnectionMultiplexer.Connect(configuration["Redis:ConnectionString"]!));

    return services;
  }
}