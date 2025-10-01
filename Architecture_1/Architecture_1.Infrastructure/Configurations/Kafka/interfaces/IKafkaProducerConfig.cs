namespace Architecture_1.Infrastructure.Configurations.Kafka.interfaces
{
    public interface IKafkaProducerConfig
    {
        string Acks { get; set; }
        int Retries { get; set; }
        int BatchSize { get; set; }
        int LingerMs { get; set; }
        string CompressionType { get; set; }
        int MaxInFlight { get; set; }
        bool EnableIdempotence { get; set; }
    }
}
