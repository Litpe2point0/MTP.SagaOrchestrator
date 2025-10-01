namespace Architecture_1.BusinessLogic.Services.MessagingServices.interfaces
{
    public interface IHandlerRegistryService
    {
        void RegisterAllHandlers();
        void UnregisterHandler(string messageType);
        Dictionary<string, Func<string, string, Task>> GetAllHandlers();
        Dictionary<string, List<string>> GetTopicMessageTypes();
        //void AddRuntimeHandler(string messageType, string topic, Func<string, string, Task> handler);
        //Task RegisterYamlHandlersAsync(string? yamlPath = null);
    }
}
