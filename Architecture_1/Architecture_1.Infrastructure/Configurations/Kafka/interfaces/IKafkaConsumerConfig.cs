namespace Architecture_1.Infrastructure.Configurations.Kafka.interfaces
{
    public interface IKafkaConsumerConfig
    {
        string GroupId { get; set; }
        string AutoOffsetReset { get; set; }
        bool EnableAutoCommit { get; set; }
        int SessionTimeoutMs { get; set; }
        int HeartbeatIntervalMs { get; set; }
        int MaxPollRecords { get; set; }
        int MaxPollIntervalMs { get; set; }
    }
}
