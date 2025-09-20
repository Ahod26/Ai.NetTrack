using backend.Extensions;
using backend.Hubs.Classes;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddOpenApi();

builder.Services.AddConfigurationOptions(builder.Configuration);

builder.Services.AddDatabase(builder.Configuration);
builder.Services.AddAuthentication(builder.Configuration);
builder.Services.AddInfrastructure();
builder.Services.AddExternalServices(builder.Configuration);
builder.Services.AddRateLimitingServices();

// business logic extensions
builder.Services.AddRepositoriesServices();
builder.Services.AddBusinessServices();



var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.UseRateLimiter();
app.UseCors("AllowReactApp");
app.UseAuthentication();
app.UseAuthorization();

app.MapHub<ChatHub>("/chathub");

app.MapControllers();

app.Run();
