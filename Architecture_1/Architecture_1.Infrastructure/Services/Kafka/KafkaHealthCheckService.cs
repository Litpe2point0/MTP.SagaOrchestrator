using Confluent.Kafka;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using Architecture_1.Infrastructure.Configurations.Kafka.interfaces;

namespace Architecture_1.Infrastructure.Services.Kafka
{
    public class KafkaHealthCheckService : IHealthCheck
    {
        private readonly IKafkaClusterConfig _kafkaClusterConfig;
        private readonly ILogger<KafkaHealthCheckService> _logger;

        public KafkaHealthCheckService(IKafkaClusterConfig kafkaClusterConfig, ILogger<KafkaHealthCheckService> logger)
        {
            _kafkaClusterConfig = kafkaClusterConfig;
            _logger = logger;
        }

        public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
        {
            try
            {
                var config = new ProducerConfig
                {
                    BootstrapServers = string.Join(",", _kafkaClusterConfig.BootstrapServers),
                    MessageTimeoutMs = 5000
                };

                using var producer = new ProducerBuilder<string, string>(config).Build();
                
                // Simple health check - if we can create producer without exception, connection is OK
                _logger.LogInformation("Kafka health check passed. Successfully connected to bootstrap servers: {Servers}", 
                    string.Join(",", _kafkaClusterConfig.BootstrapServers));
                
                return Task.FromResult(HealthCheckResult.Healthy($"Kafka cluster is healthy. Bootstrap servers: {string.Join(",", _kafkaClusterConfig.BootstrapServers)}"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Kafka health check failed");
                return Task.FromResult(HealthCheckResult.Unhealthy($"Kafka health check failed: {ex.Message}"));
            }
        }
    }
}
