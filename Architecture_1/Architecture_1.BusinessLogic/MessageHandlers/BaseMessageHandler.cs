using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Text.Json;

namespace Architecture_1.BusinessLogic.MessageHandlers
{
    public abstract class BaseMessageHandler
    {
        protected readonly ILogger _logger;

        protected BaseMessageHandler(ILogger logger)
        {
            _logger = logger;
        }

        protected T? DeserializeMessage<T>(string messageJson) where T : class
        {
            try
            {
                Console.WriteLine($"Deserializing message: {messageJson}");
                return JsonConvert.DeserializeObject<T>(messageJson);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to deserialize message: {MessageJson}", messageJson);
                return null;
            }
        }
    }
}
