using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;

namespace backend.Extensions;

public static class RateLimitingExtension
{
  public static IServiceCollection AddRateLimitingServices(this IServiceCollection services)
  {
    services.AddRateLimiter(options =>
    {
      options.AddFixedWindowLimiter("general", config =>
      {
        config.PermitLimit = 100;
        config.Window = TimeSpan.FromMinutes(1);
        config.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        config.QueueLimit = 10;
      });

      options.AddFixedWindowLimiter("chat", config =>
      {
        config.PermitLimit = 20;
        config.Window = TimeSpan.FromMinutes(1);
        config.QueueLimit = 5;
      });

      options.AddFixedWindowLimiter("news", config =>
      {
        config.PermitLimit = 30;
        config.Window = TimeSpan.FromMinutes(1);
        config.QueueLimit = 10;
      });

      // Prevent toggling abuse for starred messages
      options.AddFixedWindowLimiter("messages", config =>
      {
        config.PermitLimit = 15;
        config.Window = TimeSpan.FromMinutes(1);
        config.QueueLimit = 10;
      });

      // 5 login attempts per 15 minutes - PER IP
      options.AddPolicy("auth", httpContext =>
      RateLimitPartition.GetFixedWindowLimiter(
        partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
        factory: _ => new FixedWindowRateLimiterOptions
        {
          PermitLimit = 5,
          Window = TimeSpan.FromMinutes(15),
          QueueLimit = 0
        }));

      options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
          RateLimitPartition.GetFixedWindowLimiter(
              partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
              factory: _ => new FixedWindowRateLimiterOptions
              {
                PermitLimit = 1000,
                Window = TimeSpan.FromHours(1)
              }));
    });

    return services;
  }
}

// General configuration for now, will be improved in the future and will be matched my need and user count