namespace Architecture_1.Infrastructure.Models.Kafka
{
    public class KafkaTopicInfo
    {
        public string Name { get; set; } = string.Empty;
        public int Partitions { get; set; } = 1;
        public int ReplicationFactor { get; set; } = 1;
    }
}
