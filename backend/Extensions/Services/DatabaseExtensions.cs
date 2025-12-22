using Microsoft.EntityFrameworkCore;
using backend.Data;

namespace backend.Extensions.Services;

public static class DatabaseExtensions
{
  public static IServiceCollection AddDatabase(this IServiceCollection services, IConfiguration configuration, IHostEnvironment environment)
  {
    // Skip database setup in test environment - tests will configure their own DB
    if (environment.EnvironmentName == "Testing")
    {
      return services;
    }

    var connectionString = configuration.GetConnectionString("AINetTrack");

    services.AddDbContextPool<ApplicationDbContext>(options =>
        options.UseMySql(
            connectionString,
            ServerVersion.AutoDetect(connectionString)
        ),
        poolSize: 100
    );

    return services;
  }
}