using System.Reflection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Architecture_1.BusinessLogic.Attributes;
using Architecture_1.BusinessLogic.Services.MessagingServices.interfaces;
using Architecture_1.BusinessLogic.MessageHandlers;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;
using Architecture_1.Common.AppConfigurations.SagaFlow.interfaces;
using Architecture_1.Common.AppConfigurations.SagaFlow;

namespace Architecture_1.BusinessLogic.Services.MessagingServices
{
    public class HandlerRegistryService : IHandlerRegistryService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<HandlerRegistryService> _logger;
        private readonly Dictionary<string, (Type HandlerType, MethodInfo Method)> _handlerMethods;
        private readonly Dictionary<string, Func<string, string, Task>> _messageHandlers;
        private readonly Dictionary<string, List<string>> _runtimeTopicMessageTypes = new();

        public HandlerRegistryService(
            IServiceProvider serviceProvider,
            ILogger<HandlerRegistryService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
            _handlerMethods = new Dictionary<string, (Type, MethodInfo)>();
            _messageHandlers = new Dictionary<string, Func<string, string, Task>>();
        }

        public Dictionary<string, Func<string, string, Task>> GetAllHandlers() => _messageHandlers;

        public Dictionary<string, List<string>> GetTopicMessageTypes()
        {
            var topicMessageTypes = new Dictionary<string, List<string>>();

            var allHandlers = CollectAllHandlers();
            foreach (var handler in allHandlers)
            {
                if (!topicMessageTypes.ContainsKey(handler.Topic))
                    topicMessageTypes[handler.Topic] = new List<string>();
                if (!topicMessageTypes[handler.Topic].Contains(handler.Attribute.MessageType))
                    topicMessageTypes[handler.Topic].Add(handler.Attribute.MessageType);
            }

            foreach (var kvp in _runtimeTopicMessageTypes)
            {
                if (!topicMessageTypes.TryGetValue(kvp.Key, out var list))
                {
                    list = new List<string>();
                    topicMessageTypes[kvp.Key] = list;
                }
                foreach (var mt in kvp.Value)
                    if (!list.Contains(mt)) list.Add(mt);
            }

            return topicMessageTypes;
        }

        public void RegisterAllHandlers()
        {
            var allHandlers = CollectAllHandlers();
            foreach (var handlerInfo in allHandlers)
                RegisterSingleHandler(handlerInfo.HandlerType, handlerInfo.Method, handlerInfo.Attribute);

            _logger.LogInformation("Completed handler registration from assembly. Total handlers: {Count}", _messageHandlers.Count);
        }

        //public void AddRuntimeHandler(string messageType, string topic, Func<string, string, Task> handler)
        //{
        //    _messageHandlers[messageType] = handler;

        //    if (!_runtimeTopicMessageTypes.TryGetValue(topic, out var list))
        //    {
        //        list = new List<string>();
        //        _runtimeTopicMessageTypes[topic] = list;
        //    }
        //    if (!list.Contains(messageType)) list.Add(messageType);

        //    _logger.LogInformation("Runtime-registered handler for MessageType: {MessageType} on Topic: {Topic}", messageType, topic);
        //}
        //public async Task RegisterYamlHandlersAsync(string? yamlPath = null)
        //{
        //    try
        //    {
        //        using var scope = _serviceProvider.CreateScope();
        //        var flowConfig = scope.ServiceProvider.GetRequiredService<ISagaFlowConfig>();

        //        if (!flowConfig.Loaded || flowConfig.Flows.Count == 0)
        //        {
        //            _logger.LogInformation("SagaFlowConfig not loaded or empty. Skipping saga handler registration.");
        //            return;
        //        }

        //        // Flow listen mapping: flowName -> flow.Topic
        //        var flowHandlerType = typeof(Architecture_1.BusinessLogic.MessageHandlers.FlowMessageHandler);
        //        var flowMethod = flowHandlerType.GetMethod("HandleFlowAsync", BindingFlags.Instance | BindingFlags.Public)
        //                        ?? throw new InvalidOperationException("HandleFlowAsync not found");
        //        var flowDelegate = CreateHandlerWrapper(flowHandlerType, flowMethod);

        //        foreach (var (flowName, def) in flowConfig.Flows)
        //        {
        //            if (!string.IsNullOrWhiteSpace(def.Topic))
        //                AddRuntimeHandler(flowName, def.Topic, flowDelegate);
        //        }

        //        // Emit listen mapping: emit -> outcome.Topic (scan all steps)
        //        var emitHandlerType = typeof(Architecture_1.BusinessLogic.MessageHandlers.FlowStepEmitMessageHandler);
        //        var emitMethod = emitHandlerType.GetMethod("HandleEmitAsync", BindingFlags.Instance | BindingFlags.Public)
        //                        ?? throw new InvalidOperationException("HandleEmitAsync not found");
        //        var emitDelegate = CreateHandlerWrapper(emitHandlerType, emitMethod);

        //        foreach (var (_, def) in flowConfig.Flows)
        //        {
        //            foreach (var step in def.Steps)
        //            {
        //                if (step.OnSuccess != null && !string.IsNullOrWhiteSpace(step.OnSuccess.Emit))
        //                    AddRuntimeHandler(step.OnSuccess.Emit, step.OnSuccess.Topic, emitDelegate);
        //                if (step.OnFailure != null && !string.IsNullOrWhiteSpace(step.OnFailure.Emit))
        //                    AddRuntimeHandler(step.OnFailure.Emit, step.OnFailure.Topic, emitDelegate);
        //            }
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.LogError(ex, "Failed to register saga flow handlers from SagaFlowConfig");
        //    }

        //    await Task.CompletedTask;
        //}

        private List<(Type HandlerType, MethodInfo Method, MessageHandlerAttribute Attribute, string Topic)> CollectAllHandlers()
        {
            var result = new List<(Type, MethodInfo, MessageHandlerAttribute, string)>();

            // Resolve YAML-backed flow configuration
            using var scope = _serviceProvider.CreateScope();
            var flowConfig = scope.ServiceProvider.GetRequiredService<ISagaFlowConfig>();

            if (!flowConfig.Loaded || flowConfig.Flows.Count == 0)
            {
                _logger.LogInformation("SagaFlowConfig not loaded or empty. No saga handlers to collect.");
                return result;
            }

            // Flow start handler: FlowMessageHandler.HandleFlowAsync
            var flowHandlerType = typeof(Architecture_1.BusinessLogic.MessageHandlers.FlowMessageHandler);
            var flowMethod = flowHandlerType.GetMethod("HandleFlowAsync", BindingFlags.Instance | BindingFlags.Public)
                            ?? throw new InvalidOperationException("HandleFlowAsync not found on FlowMessageHandler");

            // Emit handler: FlowStepEmitMessageHandler.HandleEmitAsync
            var emitHandlerType = typeof(Architecture_1.BusinessLogic.MessageHandlers.FlowStepEmitMessageHandler);
            var emitMethod = emitHandlerType.GetMethod("HandleEmitAsync", BindingFlags.Instance | BindingFlags.Public)
                            ?? throw new InvalidOperationException("HandleEmitAsync not found on FlowStepEmitMessageHandler");

            // Map flows (messageType = flow name, topic = flow.Topic)
            foreach (var (flowName, def) in flowConfig.Flows)
            {
                if (string.IsNullOrWhiteSpace(def.Topic)) continue;

                var virtualAttr = new MessageHandlerAttribute(flowName, def.Topic);
                result.Add((flowHandlerType, flowMethod, virtualAttr, def.Topic));

                // Map emits under each step (messageType = emit, topic = outcome.Topic)
                foreach (var step in def.Steps)
                {
                    if (step.OnSuccess != null && !string.IsNullOrWhiteSpace(step.OnSuccess.Emit) && !string.IsNullOrWhiteSpace(step.OnSuccess.Topic))
                    {
                        var successAttr = new MessageHandlerAttribute(step.OnSuccess.Emit, step.OnSuccess.Topic);
                        result.Add((emitHandlerType, emitMethod, successAttr, step.OnSuccess.Topic));
                    }

                    if (step.OnFailure != null && !string.IsNullOrWhiteSpace(step.OnFailure.Emit) && !string.IsNullOrWhiteSpace(step.OnFailure.Topic))
                    {
                        var failureAttr = new MessageHandlerAttribute(step.OnFailure.Emit, step.OnFailure.Topic);
                        result.Add((emitHandlerType, emitMethod, failureAttr, step.OnFailure.Topic));
                    }
                }
            }

            return result;
        }

        private void RegisterSingleHandler(Type handlerType, MethodInfo method, MessageHandlerAttribute attribute)
        {
            var handlerKey = $"{attribute.MessageType}_{attribute.Topic}";
            _handlerMethods[handlerKey] = (handlerType, method);

            var wrapperDelegate = CreateHandlerWrapper(handlerType, method);
            _messageHandlers[attribute.MessageType] = wrapperDelegate;

            _logger.LogInformation(
                "Registered handler: {HandlerType}.{MethodName} for MessageType: {MessageType} on Topic: {Topic}",
                handlerType.Name, method.Name, attribute.MessageType, attribute.Topic);
        }

        private Func<string, string, Task> CreateHandlerWrapper(Type handlerType, MethodInfo method)
        {
            return async (key, messageJson) =>
            {
                using var scope = _serviceProvider.CreateScope();
                var handlerInstance = scope.ServiceProvider.GetRequiredService(handlerType);
                var task = (Task)method.Invoke(handlerInstance, new object[] { key, messageJson })!;
                await task;
            };
        }

        public void UnregisterHandler(string messageType)
        {
            _logger.LogInformation("Unregistered handler for MessageType: {MessageType}", messageType);
        }
    }
}
