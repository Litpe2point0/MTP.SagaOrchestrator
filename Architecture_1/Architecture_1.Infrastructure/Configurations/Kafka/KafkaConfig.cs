using Microsoft.Extensions.Configuration;
using Architecture_1.Infrastructure.Configurations.Kafka.interfaces;

namespace Architecture_1.Infrastructure.Configurations.Kafka
{
    public class KafkaClusterConfigModel
    {
        public List<string> BootstrapServers { get; set; } = new();
        public string ClientId { get; set; } = string.Empty;
        public string SecurityProtocol { get; set; } = "Plaintext";
        public string SaslMechanism { get; set; } = "Plain";
        public string SaslUsername { get; set; } = string.Empty;
        public string SaslPassword { get; set; } = string.Empty;
        public int ConnectionRetries { get; set; } = 3;
        public int ConnectionTimeoutMs { get; set; } = 30000;
        public int RequestTimeoutMs { get; set; } = 30000;
    }

    public class KafkaClusterConfig : IKafkaClusterConfig
    {
        public List<string> BootstrapServers { get; set; } = new();
        public string ClientId { get; set; } = "onlinehelpdesk-service";
        public string SecurityProtocol { get; set; } = "Plaintext";
        public string SaslMechanism { get; set; } = "Plain";
        public string SaslUsername { get; set; } = string.Empty;
        public string SaslPassword { get; set; } = string.Empty;
        public int ConnectionRetries { get; set; } = 3;
        public int ConnectionTimeoutMs { get; set; } = 30000;
        public int RequestTimeoutMs { get; set; } = 30000;

        public KafkaClusterConfig(IConfiguration configuration)
        {
            var kafkaConfig = configuration.GetSection("Infrastructure:Kafka:Cluster").Get<KafkaClusterConfigModel>();
            BootstrapServers = kafkaConfig?.BootstrapServers ?? new List<string> { "localhost:9092" };
            ClientId = kafkaConfig?.ClientId ?? "onlinehelpdesk-service";
            SecurityProtocol = kafkaConfig?.SecurityProtocol ?? "Plaintext";
            SaslMechanism = kafkaConfig?.SaslMechanism ?? "Plain";
            SaslUsername = kafkaConfig?.SaslUsername ?? string.Empty;
            SaslPassword = kafkaConfig?.SaslPassword ?? string.Empty;
            ConnectionRetries = kafkaConfig?.ConnectionRetries ?? 3;
            ConnectionTimeoutMs = kafkaConfig?.ConnectionTimeoutMs ?? 30000;
            RequestTimeoutMs = kafkaConfig?.RequestTimeoutMs ?? 30000;
        }
    }
}
