using Microsoft.Extensions.Logging;
using Architecture_1.BusinessLogic.Services.MessagingServices.interfaces;
using Architecture_1.Infrastructure.Services.Kafka;
using Architecture_1.Infrastructure.Models.Kafka;

namespace Architecture_1.BusinessLogic.Services.MessagingServices
{
    public class MessagingService : IMessagingService
    {
        private readonly KafkaProducerService _kafkaProducer;
        private readonly ILogger<MessagingService> _logger;

        public MessagingService(KafkaProducerService kafkaProducer, ILogger<MessagingService> logger)
        {
            _kafkaProducer = kafkaProducer;
            _logger = logger;
        }

        #region Core Methods - Single simplified method

        // Single core method with nullable key and topic
        public async Task<bool> SendMessageAsync<T>(T message, string? key = null, string? topic = null) where T : BaseMessage
        {
            // Resolve topic if not provided
            var resolvedTopic = topic ?? ResolveTopicFromMessageType(typeof(T).Name);
            
            return await SendMessageCoreAsync(message, key, resolvedTopic, null, 1);
        }

        public async Task<bool> SendSagaMessageAsync<T>(T message, string topic, string? key = null) where T : BaseMessage
        {
            return await SendSagaMessageCoreAsync(message, key, topic, 1);
        }
        #endregion

        #region Convenience Methods - Delegate to Core

        public async Task<bool> SendMessageToTopicAsync<T>(T message, string topic, string? key = null) where T : BaseMessage
            => await SendMessageAsync(message, key, topic);

        public async Task<bool> SendMessageWithHeadersAsync<T>(T message, Dictionary<string, string> headers, string? key = null, string? topic = null) where T : BaseMessage
        {
            var resolvedTopic = topic ?? ResolveTopicFromMessageType(typeof(T).Name);
            return await SendMessageCoreAsync(message, key, resolvedTopic, headers, 1);
        }

        public async Task<bool> SendMessageWithRetryAsync<T>(T message, int maxRetries = 3, string? key = null, string? topic = null) where T : BaseMessage
        {
            var resolvedTopic = topic ?? ResolveTopicFromMessageType(typeof(T).Name);
            return await SendMessageCoreAsync(message, key, resolvedTopic, null, maxRetries);
        }

        public Task SendMessageFireAndForgetAsync<T>(T message, string? key = null, string? topic = null) where T : BaseMessage
        {
            var resolvedTopic = topic ?? ResolveTopicFromMessageType(typeof(T).Name);
            _ = Task.Run(() => SendMessageCoreAsync(message, key, resolvedTopic, null, 1));
            return Task.CompletedTask;
        }

        #endregion

        #region Batch Methods

        public async Task<bool> SendMessagesAsync<T>(IEnumerable<(T message, string key)> messages, string? topic = null) where T : BaseMessage
        {
            if (messages?.Any() != true) return false;
            
            var resolvedTopic = topic ?? ResolveTopicFromMessageType(typeof(T).Name);
            var results = await Task.WhenAll(
                messages.Select(m => SendMessageCoreAsync(m.message, m.key, resolvedTopic, null, 1))
            );
            
            return results.All(r => r);
        }

        public async Task<bool> SendMessagesAsync<T>(IEnumerable<T> messages, string? topic = null) where T : BaseMessage
        {
            if (messages?.Any() != true) return false;
            
            var resolvedTopic = topic ?? ResolveTopicFromMessageType(typeof(T).Name);
            var results = await Task.WhenAll(
                messages.Select(m => SendMessageCoreAsync(m, null, resolvedTopic, null, 1))
            );
            
            return results.All(r => r);
        }

        #endregion

        #region Single Core Implementation

        /// <summary>
        /// Core method - all others delegate to this
        /// Supports null key for random partition assignment
        /// </summary>
        private async Task<bool> SendMessageCoreAsync<T>(
            T message, 
            string? key,  // Now nullable
            string topic, 
            Dictionary<string, string>? headers = null, 
            int maxRetries = 1) where T : BaseMessage
        {
            // Validation (removed key validation since it can be null now)
            if (!ValidateInput(message) || string.IsNullOrWhiteSpace(topic))
                return false;

            // Preparation
            PrepareMessage(message, headers);

            // Retry logic
            for (int attempt = 1; attempt <= maxRetries; attempt++)
            {
                try
                {
                    var result = await _kafkaProducer.SendMessageAsync(topic, key, message);
                    
                    if (result.Success)
                    {
                        LogSuccess(typeof(T).Name, topic, key, message.MessageId, attempt, maxRetries);
                        return true;
                    }
                    
                    LogFailure(typeof(T).Name, topic, key, result.ErrorMessage, attempt, maxRetries);
                }
                catch (Exception ex)
                {
                    LogException(ex, typeof(T).Name, topic, key, attempt, maxRetries);
                }

                if (attempt < maxRetries)
                {
                    var delay = TimeSpan.FromMilliseconds(Math.Pow(2, attempt) * 1000);
                    await Task.Delay(delay);
                }
            }

            return false;
        }

        private async Task<bool> SendSagaMessageCoreAsync<T>(
            T message,
            string? key,  // Now nullable
            string topic,
            int maxRetries = 1) where T : BaseMessage
        {
            // Validation (removed key validation since it can be null now)
            if (!ValidateInput(message) || string.IsNullOrWhiteSpace(topic))
                return false;

            // Retry logic
            for (int attempt = 1; attempt <= maxRetries; attempt++)
            {
                try
                {
                    var result = await _kafkaProducer.SendSagaMessageAsync(topic, key, message);

                    if (result.Success)
                    {
                        LogSuccess(typeof(T).Name, topic, key, message.MessageId, attempt, maxRetries);
                        return true;
                    }

                    LogFailure(typeof(T).Name, topic, key, result.ErrorMessage, attempt, maxRetries);
                }
                catch (Exception ex)
                {
                    LogException(ex, typeof(T).Name, topic, key, attempt, maxRetries);
                }

                if (attempt < maxRetries)
                {
                    var delay = TimeSpan.FromMilliseconds(Math.Pow(2, attempt) * 1000);
                    await Task.Delay(delay);
                }
            }

            return false;
        }

        #endregion

        #region Helper Methods

        private void PrepareMessage<T>(T message, Dictionary<string, string>? headers = null) where T : BaseMessage
        {
            message.MessageId ??= Guid.NewGuid().ToString();
            message.Timestamp = message.Timestamp == default ? DateTime.UtcNow : message.Timestamp;
            message.MessageType ??= typeof(T).Name;

            // Add custom headers if provided
            if (headers != null)
            {
                foreach (var header in headers)
                {
                    message.Metadata[header.Key] = header.Value;
                }
            }
        }

        // Updated: Remove key validation since it can be null
        private bool ValidateInput<T>(T message) where T : BaseMessage
        {
            if (message == null)
            {
                _logger.LogWarning("Message is null for MessageType: {MessageType}", typeof(T).Name);
                return false;
            }

            return true;
        }

        private string ResolveTopicFromMessageType(string messageType) => messageType.ToLower() switch
        {
            var type when type.Contains("user") => "user-events",
            var type when type.Contains("facility") => "facility-events",
            var type when type.Contains("order") => "order-events",
            var type when type.Contains("notification") => "notification-events",
            var type when type.Contains("inventory") => "inventory-events",
            var type when type.Contains("payment") => "payment-events",
            var type when type.Contains("audit") => "audit-events",
            var type when type.Contains("system") => "system-events",
            _ => "default-events"
        };

        #endregion

        #region Logging Methods

        private void LogSuccess(string messageType, string topic, string? key, string messageId, int attempt, int maxRetries)
        {
            var keyInfo = key != null ? $"Key: {key}" : "Key: null (random partition)";
            
            if (attempt == 1)
                _logger.LogDebug("Message sent successfully - MessageType: {MessageType}, Topic: {Topic}, {KeyInfo}, MessageId: {MessageId}", 
                    messageType, topic, keyInfo, messageId);
            else
                _logger.LogInformation("Message sent successfully on attempt {Attempt}/{MaxRetries} - MessageType: {MessageType}, {KeyInfo}", 
                    attempt, maxRetries, messageType, keyInfo);
        }

        private void LogFailure(string messageType, string topic, string? key, string error, int attempt, int maxRetries)
        {
            var keyInfo = key != null ? $"Key: {key}" : "Key: null (random partition)";
            _logger.LogError("Failed to send message (attempt {Attempt}/{MaxRetries}) - MessageType: {MessageType}, Topic: {Topic}, {KeyInfo}, Error: {Error}", 
                attempt, maxRetries, messageType, topic, keyInfo, error);
        }

        private void LogException(Exception ex, string messageType, string topic, string? key, int attempt, int maxRetries)
        {
            var keyInfo = key != null ? $"Key: {key}" : "Key: null (random partition)";
            _logger.LogError(ex, "Exception while sending message (attempt {Attempt}/{MaxRetries}) - MessageType: {MessageType}, Topic: {Topic}, {KeyInfo}", 
                attempt, maxRetries, messageType, topic, keyInfo);
        }

        #endregion
    }
}
