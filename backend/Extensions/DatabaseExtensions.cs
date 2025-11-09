using Microsoft.EntityFrameworkCore;
using backend.Data;

namespace backend.Extensions;

public static class DatabaseExtensions
{
  public static IServiceCollection AddDatabase(this IServiceCollection services, IConfiguration configuration)
  {
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