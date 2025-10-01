namespace Architecture_1.Infrastructure.Models.Kafka
{
    public class KafkaMessageResult
    {
        public bool Success { get; set; } = false;
        public string MessageId { get; set; } = string.Empty;
        public string Topic { get; set; } = string.Empty;
        public int Partition { get; set; } = 0;
        public long Offset { get; set; } = 0;
        public string ErrorMessage { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }
}
