namespace Architecture_1.Infrastructure.Configurations.Kafka.interfaces
{
    public interface IKafkaClusterConfig
    {
        List<string> BootstrapServers { get; set; }
        string ClientId { get; set; }
        string SecurityProtocol { get; set; }
        string SaslMechanism { get; set; }
        string SaslUsername { get; set; }
        string SaslPassword { get; set; }
        int ConnectionRetries { get; set; }
        int ConnectionTimeoutMs { get; set; }
        int RequestTimeoutMs { get; set; }
    }
}
