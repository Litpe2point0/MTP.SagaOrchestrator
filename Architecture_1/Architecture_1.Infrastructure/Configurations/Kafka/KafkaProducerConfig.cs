using Microsoft.Extensions.Configuration;
using Architecture_1.Infrastructure.Configurations.Kafka.interfaces;

namespace Architecture_1.Infrastructure.Configurations.Kafka
{
    public class KafkaProducerConfigModel
    {
        public string Acks { get; set; } = "All";
        public int Retries { get; set; } = 3;
        public int BatchSize { get; set; } = 16384;
        public int LingerMs { get; set; } = 100;
        public string CompressionType { get; set; } = "Gzip";
        public int MaxInFlight { get; set; } = 5;
        public bool EnableIdempotence { get; set; } = true;
    }

    public class KafkaProducerConfig : IKafkaProducerConfig
    {
        public string Acks { get; set; } = "All";
        public int Retries { get; set; } = 3;
        public int BatchSize { get; set; } = 16384;
        public int LingerMs { get; set; } = 100;
        public string CompressionType { get; set; } = "Gzip";
        public int MaxInFlight { get; set; } = 5;
        public bool EnableIdempotence { get; set; } = true;

        public KafkaProducerConfig(IConfiguration configuration)
        {
            var producerConfig = configuration.GetSection("Infrastructure:Kafka:Producer").Get<KafkaProducerConfigModel>();
            Acks = producerConfig?.Acks ?? "All";
            Retries = producerConfig?.Retries ?? 3;
            BatchSize = producerConfig?.BatchSize ?? 16384;
            LingerMs = producerConfig?.LingerMs ?? 100;
            CompressionType = producerConfig?.CompressionType ?? "Gzip";
            MaxInFlight = producerConfig?.MaxInFlight ?? 5;
            EnableIdempotence = producerConfig?.EnableIdempotence ?? true;
        }
    }
}
