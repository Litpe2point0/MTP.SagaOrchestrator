using System.Text.Json;
using Architecture_1.BusinessLogic.Services.MessagingServices.interfaces;
using Architecture_1.BusinessLogic.Services.SagaServices.interfaces;
using Architecture_1.Common.AppConfigurations.SagaFlow.interfaces;
using Architecture_1.DataAccess.Entities;
using Architecture_1.Infrastructure.Models.Kafka;
using Microsoft.Extensions.Logging;

namespace Architecture_1.BusinessLogic.MessageHandlers
{
    public class FlowMessageHandler : BaseMessageHandler
    {
        private readonly IMessagingService _messaging;
        private readonly ISagaFlowConfig _flowConfig;
        private readonly ISagaService _sagaService;

        public FlowMessageHandler(
            IMessagingService messaging,
            ISagaFlowConfig flowConfig,
            ISagaService sagaService,
            ILogger<FlowMessageHandler> logger) : base(logger)
        {
            _messaging = messaging;
            _flowConfig = flowConfig;
            _sagaService = sagaService;
        }

        // Invoked via registry wrapper (same signature as Facility handlers)
        public async Task HandleFlowAsync(string key, string messageJson)
        {
            try
            {
                var (flowName, sagaId, data) = ExtractFlowStart(messageJson);
                if (string.IsNullOrWhiteSpace(flowName))
                {
                    _logger.LogWarning("FlowMessageHandler: missing flowName/MessageType in payload");
                    return;
                }

                if (!_flowConfig.Loaded || !_flowConfig.Flows.TryGetValue(flowName, out var flowDef) || flowDef.Steps.Count == 0)
                {
                    _logger.LogWarning("FlowMessageHandler: unknown or empty flow '{Flow}'", flowName);
                    return;
                }

                var first = flowDef.Steps[0];
                var id = sagaId ?? Guid.NewGuid();

                // Create saga instance and first step execution
                await _sagaService.CreateSagaInstanceAsync(id, flowName, data, first.Name);
                await _sagaService.CreateStepExecutionAsync(id, first.Name, first.Topic, data);

                var cmd = new SagaCommandMessage
                {
                    SagaId = id,
                    FlowName = flowName,
                    MessageName = first.Name,
                    Data = data,
                    MessageType = first.Name
                };

                await _messaging.SendSagaMessageAsync(cmd, first.Topic, key);
                _logger.LogInformation("Flow '{Flow}' started -> first step '{Step}' to '{Topic}' (SagaId: {SagaId})",
                    flowName, first.Name, first.Topic, id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "FlowMessageHandler failed");
                throw;
            }
        }

        private static (string flowName, Guid? sagaId, Dictionary<string, object> data) ExtractFlowStart(string json)
        {
            try
            {
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;

                // Prefer MessageType (how registry routes), fallback FlowName
                var flowName = root.TryGetProperty("MessageType", out var mt) ? mt.GetString() ?? "" : "";
                if (string.IsNullOrWhiteSpace(flowName) && root.TryGetProperty("MessageName", out var fn))
                    flowName = fn.GetString() ?? "";

                Guid? sagaId = null;
                if (root.TryGetProperty("SagaId", out var sid) && sid.ValueKind == JsonValueKind.String && Guid.TryParse(sid.GetString(), out var g))
                    sagaId = g;

                var data = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
                if (root.TryGetProperty("InitialData", out var dataEl) && dataEl.ValueKind == JsonValueKind.Object)
                {
                    foreach (var p in dataEl.EnumerateObject())
                        data[p.Name] = ConvertElement(p.Value);
                }
                else if (root.ValueKind == JsonValueKind.Object)
                {
                    foreach (var p in root.EnumerateObject())
                        if (p.Name is not ("MessageType" or "FlowName" or "SagaId"))
                            data[p.Name] = ConvertElement(p.Value);
                }

                return (flowName, sagaId, data);
            }
            catch
            {
                return ("", null, new Dictionary<string, object>());
            }
        }

        private static object? ConvertElement(JsonElement el) => el.ValueKind switch
        {
            JsonValueKind.Object => el.EnumerateObject().ToDictionary(p => p.Name, p => ConvertElement(p.Value)!),
            JsonValueKind.Array => el.EnumerateArray().Select(ConvertElement).ToList(),
            JsonValueKind.String => el.GetString(),
            JsonValueKind.Number => el.TryGetInt64(out var l) ? l : el.TryGetDouble(out var d) ? d : (object?)el.GetRawText(),
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.Null => null,
            _ => el.GetRawText()
        };
    }
}