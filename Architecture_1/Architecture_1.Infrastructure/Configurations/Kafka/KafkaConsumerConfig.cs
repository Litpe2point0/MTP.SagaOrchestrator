using Microsoft.Extensions.Configuration;
using Architecture_1.Infrastructure.Configurations.Kafka.interfaces;

namespace Architecture_1.Infrastructure.Configurations.Kafka
{
    public class KafkaConsumerConfigModel
    {
        public string GroupId { get; set; } = "onlinehelpdesk-consumer-group";
        public string AutoOffsetReset { get; set; } = "Latest";
        public bool EnableAutoCommit { get; set; } = false;
        public int SessionTimeoutMs { get; set; } = 30000;
        public int HeartbeatIntervalMs { get; set; } = 3000;
        public int MaxPollRecords { get; set; } = 500;
        public int MaxPollIntervalMs { get; set; } = 300000;
    }

    public class KafkaConsumerConfig : IKafkaConsumerConfig
    {
        public string GroupId { get; set; } = "onlinehelpdesk-consumer-group";
        public string AutoOffsetReset { get; set; } = "Latest";
        public bool EnableAutoCommit { get; set; } = false;
        public int SessionTimeoutMs { get; set; } = 30000;
        public int HeartbeatIntervalMs { get; set; } = 3000;
        public int MaxPollRecords { get; set; } = 500;
        public int MaxPollIntervalMs { get; set; } = 300000;

        public KafkaConsumerConfig(IConfiguration configuration)
        {
            var consumerConfig = configuration.GetSection("Infrastructure:Kafka:Consumer").Get<KafkaConsumerConfigModel>();
            GroupId = consumerConfig?.GroupId ?? "onlinehelpdesk-consumer-group";
            AutoOffsetReset = consumerConfig?.AutoOffsetReset ?? "Latest";
            EnableAutoCommit = consumerConfig?.EnableAutoCommit ?? false;
            SessionTimeoutMs = consumerConfig?.SessionTimeoutMs ?? 30000;
            HeartbeatIntervalMs = consumerConfig?.HeartbeatIntervalMs ?? 3000;
            MaxPollRecords = consumerConfig?.MaxPollRecords ?? 500;
            MaxPollIntervalMs = consumerConfig?.MaxPollIntervalMs ?? 300000;
        }
    }
}
