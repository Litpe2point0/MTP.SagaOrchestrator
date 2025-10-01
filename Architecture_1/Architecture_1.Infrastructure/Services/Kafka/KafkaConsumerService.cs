using System.Text;
using System.Text.Json;
using Confluent.Kafka;
using Microsoft.Extensions.Logging;
using Architecture_1.Infrastructure.Configurations.Kafka.interfaces;

namespace Architecture_1.Infrastructure.Services.Kafka
{
    /// <summary>
    /// Singleton service providing Kafka consumer utilities
    /// Responsibilities: Create consumer, manage subscriptions, provide message processing utilities
    /// </summary>
    public class KafkaConsumerService : IDisposable
    {
        private IConsumer<string, string>? _consumer;
        private readonly IKafkaClusterConfig _kafkaClusterConfig;
        private readonly IKafkaConsumerConfig _kafkaConsumerConfig;
        private readonly ILogger<KafkaConsumerService> _logger;
        private readonly Dictionary<string, Func<string, string, Task>> _messageTypeHandlers;
        private readonly Dictionary<string, List<string>> _topicMessageTypes;
        private bool _isInitialized = false;

        public KafkaConsumerService(
            IKafkaClusterConfig kafkaClusterConfig,
            IKafkaConsumerConfig kafkaConsumerConfig,
            ILogger<KafkaConsumerService> logger)
        {
            _kafkaClusterConfig = kafkaClusterConfig;
            _kafkaConsumerConfig = kafkaConsumerConfig;
            _logger = logger;
            _messageTypeHandlers = new Dictionary<string, Func<string, string, Task>>();
            _topicMessageTypes = new Dictionary<string, List<string>>();

            _logger.LogInformation("KafkaConsumerService initialized as singleton utility service");
        }

        /// <summary>
        /// Initialize the Kafka consumer (called by HandlerRegistrationHostedService)
        /// </summary>
        public Task InitializeConsumerAsync()
        {
            if (_isInitialized)
            {
                _logger.LogWarning("Consumer already initialized");
                return Task.CompletedTask;
            }

            try
            {
                var config = new ConsumerConfig
                {
                    BootstrapServers = string.Join(",", _kafkaClusterConfig.BootstrapServers),
                    GroupId = _kafkaConsumerConfig.GroupId,
                    ClientId = _kafkaClusterConfig.ClientId,
                    SecurityProtocol = Enum.Parse<SecurityProtocol>(_kafkaClusterConfig.SecurityProtocol),
                    SaslMechanism = Enum.Parse<SaslMechanism>(_kafkaClusterConfig.SaslMechanism),
                    SaslUsername = _kafkaClusterConfig.SaslUsername,
                    SaslPassword = _kafkaClusterConfig.SaslPassword,
                    AutoOffsetReset = Enum.Parse<AutoOffsetReset>(_kafkaConsumerConfig.AutoOffsetReset),
                    EnableAutoCommit = _kafkaConsumerConfig.EnableAutoCommit,
                    SessionTimeoutMs = _kafkaConsumerConfig.SessionTimeoutMs,
                    HeartbeatIntervalMs = _kafkaConsumerConfig.HeartbeatIntervalMs,
                    MaxPollIntervalMs = _kafkaConsumerConfig.MaxPollIntervalMs
                };

                _consumer = new ConsumerBuilder<string, string>(config)
                    .SetErrorHandler((_, e) => _logger.LogError("Consumer error: {Error}", e.Reason))
                    .SetLogHandler((_, log) => _logger.LogInformation("Consumer log: {Message}", log.Message))
                    .Build();

                _isInitialized = true;
                _logger.LogInformation("Kafka consumer initialized successfully with servers: {Servers}", 
                    string.Join(",", _kafkaClusterConfig.BootstrapServers));
                
                return Task.CompletedTask;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to initialize Kafka consumer");
                throw;
            }
        }

        /// <summary>
        /// Register a message type handler
        /// </summary>
        public void RegisterMessageTypeHandler(string messageType, string topic, Func<string, string, Task> handler)
        {
            _messageTypeHandlers[messageType] = handler;
            
            if (!_topicMessageTypes.ContainsKey(topic))
            {
                _topicMessageTypes[topic] = new List<string>();
            }
            
            if (!_topicMessageTypes[topic].Contains(messageType))
            {
                _topicMessageTypes[topic].Add(messageType);
            }

            _logger.LogInformation("Registered handler for MessageType: {MessageType} on Topic: {Topic}", 
                messageType, topic);
        }

        /// <summary>
        /// Subscribe to topics
        /// </summary>
        public void SubscribeToTopics()
        {
            if (_consumer != null && _topicMessageTypes.Any())
            {
                var allTopics = _topicMessageTypes.Keys.ToList();
                _consumer.Subscribe(allTopics);
                _logger.LogInformation("Subscribed to topics: {Topics}", string.Join(", ", allTopics));
            }
            else
            {
                _logger.LogInformation("No topics to subscribe or consumer not initialized");
            }
        }

        /// <summary>
        /// Consume a single message (called by HandlerRegistrationHostedService)
        /// </summary>
        public ConsumeResult<string, string>? ConsumeMessage(CancellationToken cancellationToken)
        {
            if (_consumer == null)
            {
                _logger.LogWarning("Consumer not initialized");
                return null;
            }

            try
            {
                return _consumer.Consume(cancellationToken);
            }
            catch (ConsumeException ex)
            {
                _logger.LogError(ex, "Consume error: {Error}", ex.Error.Reason);
                return null;
            }
        }

        /// <summary>
        /// Process a consumed message
        /// </summary>
        public async Task ProcessMessageAsync(ConsumeResult<string, string> result)
        {
            try
            {
                var messageType = ExtractMessageTypeFromHeader(result.Message.Headers) 
                                 ?? ExtractMessageTypeFromBody(result.Message.Value);

                if (!string.IsNullOrEmpty(messageType) && _messageTypeHandlers.ContainsKey(messageType))
                {
                    _logger.LogInformation("Processing message - Topic: {Topic}, Partition: {Partition}, Offset: {Offset}, MessageType: {MessageType}", 
                        result.Topic, result.Partition.Value, result.Offset.Value, messageType);

                    await _messageTypeHandlers[messageType](result.Message.Key, result.Message.Value);
                    
                    _logger.LogInformation("Message processed successfully - MessageType: {MessageType}", messageType);
                }
                else
                {
                    _logger.LogWarning("No handler found for MessageType: {MessageType} from Topic: {Topic}", 
                        messageType ?? "Unknown", result.Topic);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing message from Topic: {Topic}, Partition: {Partition}, Offset: {Offset}", 
                    result.Topic, result.Partition.Value, result.Offset.Value);
                throw;
            }
        }

        /// <summary>
        /// Commit message offset
        /// </summary>
        public void CommitMessage(ConsumeResult<string, string> result)
        {
            try
            {
                _consumer?.Commit(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error committing message offset");
            }
        }

        /// <summary>
        /// Get registered handlers for external access
        /// </summary>
        public IReadOnlyDictionary<string, Func<string, string, Task>> GetMessageTypeHandlers()
        {
            return _messageTypeHandlers.AsReadOnly();
        }

        /// <summary>
        /// Get topic-message type mappings for external access
        /// </summary>
        public IReadOnlyDictionary<string, List<string>> GetTopicMessageTypes()
        {
            return _topicMessageTypes.ToDictionary(x => x.Key, x => x.Value.ToList()).AsReadOnly();
        }

        private string? ExtractMessageTypeFromHeader(Headers headers)
        {
            if (headers != null && headers.TryGetLastBytes("MessageName", out var messageTypeBytes))
            {
                return Encoding.UTF8.GetString(messageTypeBytes);
            }
            return null;
        }

        private string? ExtractMessageTypeFromBody(string messageBody)
        {
            try
            {
                using var document = JsonDocument.Parse(messageBody);
                if (document.RootElement.TryGetProperty("MessageName", out var messageTypeElement))
                {
                    return messageTypeElement.GetString();
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to extract message type from body");
            }
            return null;
        }

        public void Dispose()
        {
            try
            {
                if (_consumer != null)
                {
                    _consumer.Close();
                    _consumer.Dispose();
                }
                _logger.LogInformation("KafkaConsumerService disposed successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error disposing KafkaConsumerService");
            }
        }
    }
}
