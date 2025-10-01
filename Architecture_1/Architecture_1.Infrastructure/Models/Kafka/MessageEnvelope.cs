namespace Architecture_1.Infrastructure.Models.Kafka
{
    public class MessageEnvelope<T> where T : BaseMessage
    {
        public string MessageType { get; set; } = typeof(T).Name;
        public string MessageId { get; set; } = Guid.NewGuid().ToString();
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public string CorrelationId { get; set; } = string.Empty;
        public T Data { get; set; } = default!;
        public Dictionary<string, string> Metadata { get; set; } = new();
    }
}
