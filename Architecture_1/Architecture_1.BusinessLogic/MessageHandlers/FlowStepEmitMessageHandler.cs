using System.Text.Json;
using Architecture_1.BusinessLogic.Services.MessagingServices.interfaces;
using Architecture_1.BusinessLogic.Services.SagaServices.interfaces;
using Architecture_1.Common.AppConfigurations.SagaFlow.interfaces;
using Architecture_1.DataAccess.Entities;
using Architecture_1.Infrastructure.Models.Kafka;
using Microsoft.Extensions.Logging;

namespace Architecture_1.BusinessLogic.MessageHandlers
{
    public class FlowStepEmitMessageHandler : BaseMessageHandler
    {
        private readonly IMessagingService _messaging;
        private readonly ISagaFlowConfig _flowConfig;
        private readonly ISagaService _sagaService;

        public FlowStepEmitMessageHandler(
            IMessagingService messaging,
            ISagaFlowConfig flowConfig,
            ISagaService sagaService,
            ILogger<FlowStepEmitMessageHandler> logger) : base(logger)
        {
            _messaging = messaging;
            _flowConfig = flowConfig;
            _sagaService = sagaService;
        }

        // Invoked via registry wrapper (same signature as Facility handlers)
        public async Task HandleEmitAsync(string key, string messageJson)
        {
            try
            {
                var (emit, sagaId, flowName, data) = ExtractEmit(messageJson);
                if (string.IsNullOrWhiteSpace(emit))
                {
                    _logger.LogWarning("FlowStepEmitMessageHandler: missing emit/MessageType in payload");
                    return;
                }

                if (!_flowConfig.Loaded || _flowConfig.Flows.Count == 0)
                {
                    _logger.LogWarning("FlowStepEmitMessageHandler: flow config not loaded");
                    return;
                }

                var outcome = FindOutcomeByEmit(emit);
                if (outcome == null)
                {
                    _logger.LogWarning("Emit '{Emit}' not found in any flow", emit);
                    return;
                }

                var currentSagaId = sagaId ?? Guid.NewGuid();

                // Determine if this is success or failure based on emit name
                var isSuccess = emit.EndsWith(".success", StringComparison.OrdinalIgnoreCase);
                var isFailure = emit.EndsWith(".failed", StringComparison.OrdinalIgnoreCase) || emit.EndsWith(".failure", StringComparison.OrdinalIgnoreCase);

                // Extract step name from emit (e.g., "create-order.success" -> "create-order")
                var stepName = emit.Contains('.') ? emit.Substring(0, emit.LastIndexOf('.')) : emit;

                if (isSuccess)
                {
                    // Update step execution status to SUCCESS
                    await _sagaService.UpdateStepExecutionStatusAsync(currentSagaId, stepName, StepStatus.SUCCESS, data);

                    // Update saga result data
                    await _sagaService.UpdateSagaStatusAsync(currentSagaId, SagaStatus.RUNNING, data);

                    // 1) Fan-out next steps
                    foreach (var step in outcome.Value.NextSteps)
                    {
                        // Create step execution for next step
                        await _sagaService.CreateStepExecutionAsync(currentSagaId, step.Name, step.Topic, data);

                        // Update saga current step
                        await _sagaService.UpdateSagaCurrentStepAsync(currentSagaId, step.Name);

                        var cmd = new SagaCommandMessage
                        {
                            SagaId = currentSagaId,
                            FlowName = string.IsNullOrWhiteSpace(flowName) ? "(unknown)" : flowName!,
                            MessageName = step.Name,
                            Data = data,
                            MessageType = step.Name
                        };

                        await _messaging.SendSagaMessageAsync(cmd, step.Topic, key);
                        _logger.LogInformation("Emit '{Emit}' -> step '{Step}' sent to '{Topic}' (SagaId: {SagaId})",
                            emit, step.Name, step.Topic, currentSagaId);
                    }

                    // Check if no more next steps - check for saga completion
                    if (outcome.Value.NextSteps.Count == 0)
                    {
                        await _sagaService.UpdateSagaCurrentStepAsync(currentSagaId, null);
                        await _sagaService.CheckAndUpdateSagaCompletionAsync(currentSagaId);
                    }

                    // 2) Start next flow (single path)
                    if (!string.IsNullOrWhiteSpace(outcome.Value.NextFlow))
                    {
                        var nf = outcome.Value.NextFlow!;
                        if (!_flowConfig.Flows.TryGetValue(nf, out var flowDef) || string.IsNullOrWhiteSpace(flowDef.Topic))
                        {
                            _logger.LogWarning("Emit '{Emit}' -> unknown next flow '{Flow}'", emit, nf);
                        }
                        else
                        {
                            var start = new StartSagaTriggerMessage
                            {
                                MessageName = nf,
                                Data = data,
                                MessageType = nf
                            };

                            await _messaging.SendSagaMessageAsync(start, flowDef.Topic, key);
                            _logger.LogInformation("Emit '{Emit}' -> start flow '{Flow}' to '{Topic}'",
                                emit, nf, flowDef.Topic);
                        }
                    }
                }
                else if (isFailure)
                {
                    // Update step execution status to FAILED
                    await _sagaService.UpdateStepExecutionStatusAsync(currentSagaId, stepName, StepStatus.FAILED, data, $"Step failed with emit: {emit}");

                    // Update saga status to FAILED
                    await _sagaService.UpdateSagaStatusAsync(currentSagaId, SagaStatus.FAILED, data, stepName, $"Saga failed at step: {stepName}");

                    _logger.LogWarning("Saga failed: {SagaId}, Step: {StepName}, Emit: {Emit}", currentSagaId, stepName, emit);
                }
                else
                {
                    _logger.LogWarning("Unknown emit type: {Emit}. Cannot determine success or failure.", emit);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "FlowStepEmitMessageHandler failed");
                throw;
            }
        }

        private (List<(string Name, string Topic)> NextSteps, string? NextFlow)? FindOutcomeByEmit(string emit)
        {
            foreach (var (_, flow) in _flowConfig.Flows)
            {
                foreach (var step in flow.Steps)
                {
                    if (step.OnSuccess != null && string.Equals(step.OnSuccess.Emit, emit, StringComparison.OrdinalIgnoreCase))
                    {
                        var steps = (step.OnSuccess.NextSteps ?? new()).Select(s => (s.Name, s.Topic)).ToList();
                        return (steps, step.OnSuccess.NextFlow);
                    }
                    if (step.OnFailure != null && string.Equals(step.OnFailure.Emit, emit, StringComparison.OrdinalIgnoreCase))
                    {
                        var steps = (step.OnFailure.NextSteps ?? new()).Select(s => (s.Name, s.Topic)).ToList();
                        return (steps, step.OnFailure.NextFlow);
                    }
                }
            }
            return null;
        }

        private static (string emit, Guid? sagaId, string? flowName, Dictionary<string, object> data) ExtractEmit(string json)
        {
            try
            {
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;

                var emit = root.TryGetProperty("MessageName", out var mt) ? mt.GetString() ?? "" : "";

                Guid? sagaId = null;
                if (root.TryGetProperty("SagaId", out var sid) && sid.ValueKind == JsonValueKind.String && Guid.TryParse(sid.GetString(), out var g))
                    sagaId = g;

                string? flowName = null;
                if (root.TryGetProperty("FlowName", out var fn) && fn.ValueKind == JsonValueKind.String)
                    flowName = fn.GetString();

                var data = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
                if (root.TryGetProperty("Data", out var dataEl) && dataEl.ValueKind == JsonValueKind.Object)
                {
                    foreach (var p in dataEl.EnumerateObject())
                        data[p.Name] = ConvertElement(p.Value);
                }
                else if (root.ValueKind == JsonValueKind.Object)
                {
                    foreach (var p in root.EnumerateObject())
                        if (p.Name is not ("MessageName" or "FlowName" or "SagaId"))
                            data[p.Name] = ConvertElement(p.Value);
                }

                return (emit, sagaId, flowName, data);
            }
            catch
            {
                return ("", null, null, new Dictionary<string, object>());
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