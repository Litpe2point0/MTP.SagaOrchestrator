using System.Text;
using System.Text.Json;
using Confluent.Kafka;
using Microsoft.Extensions.Logging;
using Architecture_1.Infrastructure.Configurations.Kafka.interfaces;
using Architecture_1.Infrastructure.Models.Kafka;

namespace Architecture_1.Infrastructure.Services.Kafka
{
    public class KafkaProducerService : IDisposable
    {
        private readonly IProducer<string, string> _producer;
        private readonly IKafkaClusterConfig _kafkaClusterConfig;
        private readonly IKafkaProducerConfig _kafkaProducerConfig;
        private readonly ILogger<KafkaProducerService> _logger;

        public KafkaProducerService(
            IKafkaClusterConfig kafkaClusterConfig,
            IKafkaProducerConfig kafkaProducerConfig,
            ILogger<KafkaProducerService> logger)
        {
            _kafkaClusterConfig = kafkaClusterConfig;
            _kafkaProducerConfig = kafkaProducerConfig;
            _logger = logger;

            var config = new ProducerConfig
            {
                BootstrapServers = string.Join(",", _kafkaClusterConfig.BootstrapServers),
                ClientId = _kafkaClusterConfig.ClientId,
                SecurityProtocol = Enum.Parse<SecurityProtocol>(_kafkaClusterConfig.SecurityProtocol),
                SaslMechanism = Enum.Parse<SaslMechanism>(_kafkaClusterConfig.SaslMechanism),
                SaslUsername = _kafkaClusterConfig.SaslUsername,
                SaslPassword = _kafkaClusterConfig.SaslPassword,
                Acks = _kafkaProducerConfig.Acks == "All" ? Acks.All : Acks.Leader,
                EnableIdempotence = _kafkaProducerConfig.EnableIdempotence
            };

            _producer = new ProducerBuilder<string, string>(config)
                .SetErrorHandler((_, e) => _logger.LogError("Producer error: {Error}", e.Reason))
                .SetLogHandler((_, log) => _logger.LogInformation("Producer log: {Message}", log.Message))
                .Build();

            _logger.LogInformation("KafkaProducerService initialized with bootstrap servers: {Servers}", 
                string.Join(",", _kafkaClusterConfig.BootstrapServers));
        }

        public async Task<KafkaMessageResult> SendMessageAsync<T>(string topic, string? key, T message) 
            where T : BaseMessage
        {
            try
            {
                var envelope = new MessageEnvelope<T>
                {
                    MessageType = message.MessageType,
                    MessageId = message.MessageId,
                    Timestamp = message.Timestamp,
                    CorrelationId = message.CorrelationId,
                    Data = message,
                    Metadata = message.Metadata
                };

                var jsonMessage = JsonSerializer.Serialize(envelope);
                var kafkaMessage = new Message<string, string>
                {
                    Key = key!, // Can be null for random partitioning
                    Value = jsonMessage,
                    Timestamp = new Timestamp(DateTime.UtcNow),
                    Headers = new Headers
                    {
                        { "MessageType", Encoding.UTF8.GetBytes(message.MessageType) },
                        { "MessageId", Encoding.UTF8.GetBytes(message.MessageId) },
                        { "CorrelationId", Encoding.UTF8.GetBytes(message.CorrelationId) }
                    }
                };

                var result = await _producer.ProduceAsync(topic, kafkaMessage);
                
                var partitionStrategy = key != null ? "keyed partitioning" : "random partitioning";
                _logger.LogInformation("Message sent successfully - Topic: {Topic}, Partition: {Partition}, Offset: {Offset}, MessageType: {MessageType}, Strategy: {Strategy}", 
                    result.Topic, result.Partition.Value, result.Offset.Value, message.MessageType, partitionStrategy);

                return new KafkaMessageResult
                {
                    Success = true,
                    MessageId = message.MessageId,
                    Topic = result.Topic,
                    Partition = result.Partition.Value,
                    Offset = result.Offset.Value,
                    Timestamp = DateTime.UtcNow
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send message to topic {Topic}, MessageType: {MessageType}", 
                    topic, typeof(T).Name);
                
                return new KafkaMessageResult
                {
                    Success = false,
                    ErrorMessage = ex.Message,
                    Topic = topic,
                    MessageId = message.MessageId,
                    Timestamp = DateTime.UtcNow
                };
            }
        }

        public async Task<KafkaMessageResult> SendSagaMessageAsync<T>(string topic, string? key, T message)
            where T : BaseMessage
        {
            try
            {
                var jsonMessage = JsonSerializer.Serialize(message);
                var kafkaMessage = new Message<string, string>
                {
                    Key = key!, // Can be null for random partitioning
                    Value = jsonMessage,
                    Timestamp = new Timestamp(DateTime.UtcNow),
                    Headers = new Headers
                    {
                        { "MessageType", Encoding.UTF8.GetBytes(message.MessageType) },
                        { "MessageId", Encoding.UTF8.GetBytes(message.MessageId) },
                        { "CorrelationId", Encoding.UTF8.GetBytes(message.CorrelationId) }
                    }
                };

                var result = await _producer.ProduceAsync(topic, kafkaMessage);

                var partitionStrategy = key != null ? "keyed partitioning" : "random partitioning";
                _logger.LogInformation("Message sent successfully - Topic: {Topic}, Partition: {Partition}, Offset: {Offset}, MessageType: {MessageType}, Strategy: {Strategy}",
                    result.Topic, result.Partition.Value, result.Offset.Value, message.MessageType, partitionStrategy);

                return new KafkaMessageResult
                {
                    Success = true,
                    MessageId = message.MessageId,
                    Topic = result.Topic,
                    Partition = result.Partition.Value,
                    Offset = result.Offset.Value,
                    Timestamp = DateTime.UtcNow
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send message to topic {Topic}, MessageType: {MessageType}",
                    topic, typeof(T).Name);

                return new KafkaMessageResult
                {
                    Success = false,
                    ErrorMessage = ex.Message,
                    Topic = topic,
                    MessageId = message.MessageId,
                    Timestamp = DateTime.UtcNow
                };
            }
        }

        public void Dispose()
        {
            try
            {
                _producer?.Flush(TimeSpan.FromSeconds(10));
                _producer?.Dispose();
                _logger.LogInformation("KafkaProducerService disposed successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error disposing KafkaProducerService");
            }
        }
    }
}
