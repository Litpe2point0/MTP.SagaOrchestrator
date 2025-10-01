using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Architecture_1.API.Configurations.Builder
{
    public static class HealthCheckConfig
    {
        public static void AddBuilderHealthCheckConfig(this WebApplicationBuilder builder)
        {
            builder.Services.AddHealthChecks()
                .AddCheck("self", () => HealthCheckResult.Healthy("Service is running"))
                .AddCheck("database", () => {
                    // Kiểm tra database connection
                    try 
                    {
                        // TODO: Thực hiện kiểm tra database thực tế
                        return HealthCheckResult.Healthy("Database is accessible");
                    }
                    catch (Exception ex)
                    {
                        return HealthCheckResult.Unhealthy($"Database check failed: {ex.Message}");
                    }
                })
                .AddCheck("redis", () => {
                    // Kiểm tra Redis connection
                    try 
                    {
                        // TODO: Thực hiện kiểm tra Redis thực tế
                        return HealthCheckResult.Healthy("Redis is accessible");
                    }
                    catch (Exception ex)
                    {
                        return HealthCheckResult.Unhealthy($"Redis check failed: {ex.Message}");
                    }
                });
        }

        
    }
}
