using System.Text.Json.Serialization;

public static class InfrastructureExtensions
{
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

    return services;
  }
}