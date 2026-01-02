using backend.Extensions.Services;
using backend.Middleware;
using backend.Hubs.Classes;
using Serilog;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.AddSerilog();

builder.Services.AddControllers();
builder.Services.AddOpenApi();

builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

builder.Services.AddConfigurationOptions(builder.Configuration);

builder.Services.AddDatabase(builder.Configuration, builder.Environment);
builder.Services.AddAuthentication(builder.Configuration);
builder.Services.AddInfrastructure();
builder.Services.AddExternalServices(builder.Configuration);
builder.Services.AddRateLimitingServices();

// business logic extensions
builder.Services.AddRepositoriesServices();
builder.Services.AddBusinessServices();



var app = builder.Build();

app.UseExceptionHandler();

app.UseSerilogRequestLogging();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.UseHttpsRedirection();

app.UseRateLimiter();

app.UseCors("AllowReactApp");

app.UseAuthentication();
app.UseAuthorization();

app.UseRequestTimeouts();

app.MapHub<ChatHub>("/chathub");

app.MapControllers();

app.Run();

// Make the implicit Program class accessible for integration tests
public partial class Program { }
