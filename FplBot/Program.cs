using FplBot.Clients;
using FplBot.Configuration;
using FplBot.Services;
using Microsoft.Extensions.Options;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

// Add health checks
builder.Services.AddHealthChecks()
    .AddCheck("self", () => Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Healthy("Application is running"));

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
// Configure API clients with environment variable override for sensitive data
builder.Services.Configure<FootballClientConfig>(config =>
{
    builder.Configuration.GetSection("FootballData").Bind(config);
    // Override with environment variables if present (for Kubernetes secrets)
    if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("FOOTBALL_DATA_BASE_URL")))
        config.BaseUrl = Environment.GetEnvironmentVariable("FOOTBALL_DATA_BASE_URL")!;
    if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("FOOTBALL_DATA_API_TOKEN")))
        config.ApiToken = Environment.GetEnvironmentVariable("FOOTBALL_DATA_API_TOKEN")!;
});

builder.Services.Configure<OddsClientConfig>(config =>
{
    builder.Configuration.GetSection("OddsAPI").Bind(config);
    // Override with environment variables if present (for Kubernetes secrets)
    if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("ODDS_API_BASE_URL")))
        config.BaseUrl = Environment.GetEnvironmentVariable("ODDS_API_BASE_URL")!;
    if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("ODDS_API_TOKEN")))
        config.ApiToken = Environment.GetEnvironmentVariable("ODDS_API_TOKEN")!;
});

builder.Services.AddHttpClient("FootballDataClient", (sp, client) =>
{
    var cfg = sp.GetRequiredService<IOptions<FootballClientConfig>>().Value;
    client.BaseAddress = new Uri(cfg.BaseUrl);
    client.DefaultRequestHeaders.Add("X-Auth-Token", cfg.ApiToken);
});

builder.Services.AddHttpClient("OddsClient", (sp, client) =>
{
    var cfg = sp.GetRequiredService<IOptions<OddsClientConfig>>().Value;
    client.BaseAddress = new Uri(cfg.BaseUrl);
});

builder.Services.AddSingleton<IFootballDataClient, FootballDataClient>();
builder.Services.AddSingleton<IOddsClient, OddsClient>();
builder.Services.AddSingleton<IFplService, FplService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();

    app.MapScalarApiReference(options =>
    {
        options.Title = "FplBot API";       
        options.OpenApiRoutePattern = "/openapi/v1.json";
    });
}

// Map health check endpoints
app.MapHealthChecks("/health");
app.MapHealthChecks("/health/ready");

// Only use HTTPS redirection in development, not in containers
if (!app.Environment.IsProduction())
{
    app.UseHttpsRedirection();
}

app.MapControllers();

app.Run();