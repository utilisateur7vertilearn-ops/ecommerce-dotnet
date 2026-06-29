using ECommerce.Gateway.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;

var builder = WebApplication.CreateBuilder(args);

// Aspire: telemetry, health checks, resilience, service discovery.
builder.AddServiceDefaults();

// YARP reverse proxy. Routes/clusters are loaded from configuration
// (appsettings.json) and destinations are resolved via Aspire service discovery.
builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"))
    .AddServiceDiscoveryDestinationResolver();

// Named clients used exclusively by the upstream health checks.
// Service discovery (wired by AddServiceDefaults) resolves the addresses at runtime.
builder.Services.AddHttpClient("catalog", c => c.BaseAddress = new Uri("https+http://catalog"));
builder.Services.AddHttpClient("ordering", c => c.BaseAddress = new Uri("https+http://ordering"));

builder.Services.AddHealthChecks()
    .Add(new HealthCheckRegistration("catalog",
        sp => new HttpServiceHealthCheck(sp.GetRequiredService<IHttpClientFactory>(), "catalog"),
        failureStatus: null, tags: ["ready"]))
    .Add(new HealthCheckRegistration("ordering",
        sp => new HttpServiceHealthCheck(sp.GetRequiredService<IHttpClientFactory>(), "ordering"),
        failureStatus: null, tags: ["ready"]));

var app = builder.Build();

app.MapDefaultEndpoints();

app.MapReverseProxy();

app.Run();
