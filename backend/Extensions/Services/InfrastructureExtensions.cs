using System.Text.Json.Serialization;
using backend.Background;
using backend.Mapping;
using Serilog;
using Serilog.Events;

namespace backend.Extensions.Services;

public static class InfrastructureExtensions
{
  public static WebApplicationBuilder AddSerilog(this WebApplicationBuilder builder)
  {
    builder.Host.UseSerilog((context, configuration) =>
    {
      configuration
        .MinimumLevel.Information()
        .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
        .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
        .MinimumLevel.Override("Microsoft.EntityFrameworkCore", LogEventLevel.Warning)
        .Enrich.FromLogContext()
        .Enrich.WithProperty("Application", "AiTrack")
        .Enrich.WithProperty("Environment", context.HostingEnvironment.EnvironmentName)
        .WriteTo.Console()
        .WriteTo.Debug()
        .WriteTo.Seq("http://localhost:5341");
    });

    return builder;
  }

  public static IServiceCollection AddInfrastructure(this IServiceCollection services)
  {
    services.AddHttpContextAccessor();

    services.AddAutoMapper(typeof(AutoMappersProfiles));

    services.ConfigureHttpJsonOptions(options =>
    {
      options.SerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
    });

    services.AddSignalR().AddJsonProtocol(options =>
    {
      options.PayloadSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
    });

    services.AddMemoryCache();

    services.AddCors(options =>
    {
      options.AddPolicy("AllowReactApp", policy =>
          {
            policy.WithOrigins("http://localhost:5173")
                    .AllowAnyHeader()
                    .AllowAnyMethod()
                    .AllowCredentials();
          });
    });

    services.AddHostedService<SeqDockerService>();
    services.AddHostedService<NewsAggregationService>();
    services.AddHttpClient();
    services.AddRequestTimeouts();

    return services;
  }
}