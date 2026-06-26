using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace ECommerce.Gateway.HealthChecks;

public class HttpServiceHealthCheck(IHttpClientFactory factory, string clientName) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            var client = factory.CreateClient(clientName);
            var response = await client.GetAsync("/health", cancellationToken);
            return response.IsSuccessStatusCode
                ? HealthCheckResult.Healthy()
                : HealthCheckResult.Unhealthy($"HTTP {(int)response.StatusCode}");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy(ex.Message);
        }
    }
}
