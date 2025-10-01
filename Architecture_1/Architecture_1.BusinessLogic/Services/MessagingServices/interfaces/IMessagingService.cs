using Architecture_1.Infrastructure.Models.Kafka;

namespace Architecture_1.BusinessLogic.Services.MessagingServices.interfaces
{
    public interface IMessagingService
    {
        #region Core Methods
        // Single core method with nullable key and topic
        Task<bool> SendMessageAsync<T>(T message, string? key = null, string? topic = null) where T : BaseMessage;
        #endregion

        #region Convenience Methods
        // With additional features
        Task<bool> SendSagaMessageAsync<T>(T message, string topic, string? key = null) where T : BaseMessage;
        Task<bool> SendMessageToTopicAsync<T>(T message, string topic, string? key = null) where T : BaseMessage;
        Task<bool> SendMessageWithHeadersAsync<T>(T message, Dictionary<string, string> headers, string? key = null, string? topic = null) where T : BaseMessage;
        Task<bool> SendMessageWithRetryAsync<T>(T message, int maxRetries = 3, string? key = null, string? topic = null) where T : BaseMessage;
        Task SendMessageFireAndForgetAsync<T>(T message, string? key = null, string? topic = null) where T : BaseMessage;
        #endregion

        #region Batch Methods
        // With keys
        Task<bool> SendMessagesAsync<T>(IEnumerable<(T message, string key)> messages, string? topic = null) where T : BaseMessage;
        
        // Without keys (random partition)
        Task<bool> SendMessagesAsync<T>(IEnumerable<T> messages, string? topic = null) where T : BaseMessage;
        #endregion
    }
}
