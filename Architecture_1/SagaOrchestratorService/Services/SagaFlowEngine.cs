using SagaOrchestratorService.Repositories;
using System.Text.Json;
using Confluent.Kafka;
using SagaOrchestratorService.Models;

namespace SagaOrchestratorService.Services
{
    public class SagaFlowEngine
    {
        private readonly ILogger<SagaFlowEngine> _logger;
        private readonly IProducer<string, string> _producer;
        private readonly ISagaRepository _sagaRepository;
        private readonly SagaFlowDefinition _flowDefinition;
        private readonly ISagaUpdateQueue _updateQueue; // Change from UpdateQueueService to ISagaUpdateQueue

        public SagaFlowEngine(
            ILogger<SagaFlowEngine> logger,
            IProducer<string, string> producer,
            ISagaRepository sagaRepository,
            SagaFlowDefinition flowDefinition,
            ISagaUpdateQueue updateQueue) // Change from UpdateQueueService to ISagaUpdateQueue
        {
            _logger = logger;
            _producer = producer;
            _sagaRepository = sagaRepository;
            _flowDefinition = flowDefinition;
            _updateQueue = updateQueue; // And update the field type
        }

        // Add this method to handle the new flow structure
        public async Task<Guid> StartSagaAsync(string flowName, Dictionary<string, object> initialData)
        {
            var allFlows = _flowDefinition.GetAllFlows();

            if (!allFlows.ContainsKey(flowName))
            {
                throw new ArgumentException($"Flow '{flowName}' not found in definition");
            }

            var sagaId = Guid.NewGuid();
            var flow = allFlows[flowName];

            var sagaInstance = new SagaInstance
            {
                SagaId = sagaId,
                FlowName = flowName,
                CurrentStep = "",
                Status = _flowDefinition.IsRollbackFlow(flowName) ? SagaStatus.RollingBack : SagaStatus.Running,
                Context = initialData
            };

            await _sagaRepository.CreateSagaInstanceAsync(sagaInstance);

            _logger.LogInformation($"Starting saga {sagaId} for flow {flowName}");

            // Always start with the first step
            var firstStep = flow.Steps.FirstOrDefault();
            if (firstStep != null)
            {
                await _updateQueue.QueueUpdateAsync(sagaId, async saga =>
                {
                    await ExecuteStepAsync(saga, firstStep);
                    return saga;
                });
            }

            return sagaId;
        }

        public async Task ProcessSagaEventAsync(SagaEvent sagaEvent)
        {
            // Load saga from database
            var sagaInstance = await _sagaRepository.GetSagaInstanceAsync(sagaEvent.SagaId);

            if (sagaInstance == null)
            {
                _logger.LogWarning($"Received event for unknown saga {sagaEvent.SagaId}");
                return;
            }

            var allFlows = _flowDefinition.GetAllFlows();
            var flow = allFlows[sagaInstance.FlowName];
            var currentStep = flow.Steps.FirstOrDefault(s => s.Name == sagaEvent.StepName);

            if (currentStep == null)
            {
                _logger.LogError($"Step {sagaEvent.StepName} not found in flow {sagaInstance.FlowName}");
                return;
            }

            if (sagaEvent.IsSuccess)
            {
                await HandleStepSuccessAsync(sagaInstance, currentStep, sagaEvent);
            }
            else
            {
                await HandleStepFailureAsync(sagaInstance, currentStep, sagaEvent);
            }
        }

        private async Task HandleStepSuccessAsync(SagaInstance sagaInstance, StepDefinition step, SagaEvent sagaEvent)
        {
            await _updateQueue.QueueUpdateAsync(sagaInstance.SagaId, async saga =>
            {
                saga.CompleteStep(step.Name, true, null, sagaEvent.Data);
                _logger.LogInformation($"Step {step.Name} completed successfully for saga {saga.SagaId}");

                AddNewPropertiesOnly(saga.Context, sagaEvent.Data);

                // Handle onSuccess outcome
                if (step.OnSuccess != null)
                {
                    await HandleStepOutcomeAsync(saga, step.OnSuccess);
                }
                else
                {
                    // Check if saga should be completed
                    await CheckSagaCompletionAsync(saga);
                }

                return saga;
            });
        }

        private async Task HandleStepFailureAsync(SagaInstance sagaInstance, StepDefinition step, SagaEvent sagaEvent)
        {
            await _updateQueue.QueueUpdateAsync(sagaInstance.SagaId, async saga =>
            {
                saga.CompleteStep(step.Name, false, sagaEvent.ErrorMessage, sagaEvent.Data);
                _logger.LogError($"Step {step.Name} failed for saga {sagaInstance.SagaId}: {sagaEvent.ErrorMessage}");

                AddNewPropertiesOnly(saga.Context, sagaEvent.Data);

                // Handle onFailure outcome
                if (step.OnFailure != null)
                {
                    await HandleStepOutcomeAsync(saga, step.OnFailure);
                }
                else
                {
                    // No failure handling defined, mark saga as failed
                    saga.CompleteSaga(false);
                    _logger.LogError($"Saga {saga.SagaId} marked as failed - no failure handling defined");
                }

                return saga;
            });
        }

        private async Task HandleStepOutcomeAsync(SagaInstance sagaInstance, StepOutcome outcome)
        {
            // Handle nextSteps - can be multiple steps executed simultaneously
            if (outcome.NextSteps != null && outcome.NextSteps.Any())
            {
                var rollbackSteps = outcome.GetRollbackSteps();
                var regularSteps = outcome.GetRegularSteps();

                // Execute rollback steps (send .command for .rollback steps)
                foreach (var rollbackStep in rollbackSteps)
                {
                    var commandName = rollbackStep.EndsWith(".rollback") 
                        ? rollbackStep.Replace(".rollback", ".command")
                        : rollbackStep + ".command";
                    
                    await ExecuteRollbackStepAsync(sagaInstance, rollbackStep, commandName);
                }

                // Execute regular steps
                foreach (var regularStep in regularSteps)
                {
                    await ExecuteNextStepAsync(sagaInstance, regularStep);
                }
            }

            // Handle nextFlows - start a new flow
            //if (!string.IsNullOrEmpty(outcome.NextFlows))
            //{
            //    await StartNewFlowAsync(sagaInstance, outcome.NextFlows);
            //}

            // If no nextSteps and no nextFlows, check for completion
            if ((outcome.NextSteps == null || !outcome.NextSteps.Any()) && 
                string.IsNullOrEmpty(outcome.NextFlows))
            {
                await CheckSagaCompletionAsync(sagaInstance);
            }
        }

        private async Task ExecuteRollbackStepAsync(SagaInstance sagaInstance, string rollbackStepName, string commandName)
        {
            _logger.LogInformation($"Executing rollback step {rollbackStepName} with command {commandName} for saga {sagaInstance.SagaId}");
            sagaInstance.StartStep(rollbackStepName);

            var command = new SagaCommand
            {
                SagaId = sagaInstance.SagaId,
                CommandType = commandName,
                StepName = rollbackStepName,
                FlowName = sagaInstance.FlowName,
                Data = sagaInstance.Context,
                Timestamp = DateTime.UtcNow
            };

            var commandJson = JsonSerializer.Serialize(command);
            var topic = GetTopicForCommand(commandName);

            await _producer.ProduceAsync(topic, new Message<string, string>
            {
                Key = sagaInstance.SagaId.ToString(),
                Value = commandJson
            });

            _logger.LogInformation($"Published rollback command {commandName} to topic {topic} for saga {sagaInstance.SagaId}");
        }

        private async Task ExecuteNextStepAsync(SagaInstance sagaInstance, string stepName)
        {
            var allFlows = _flowDefinition.GetAllFlows();
            var currentFlow = allFlows[sagaInstance.FlowName];

            //if(stepName.EndsWith("rollback"))
            //{
            //    _logger.LogWarning($"Step {stepName} indicates a rollback action. Ensure this is intended in the current flow context.");
            //    var nextStep = new StepDefinition()
            //    {
            //        Name = stepName,
            //        Command = stepName + ".command"
            //    };
            //    await ExecuteStepAsync(sagaInstance, nextStep);
            //}
            //else
            //{
                // Find the step in the current flow
                var nextStep = currentFlow.Steps.FirstOrDefault(s => s.Name == stepName);

                if (nextStep != null)
                {
                    await ExecuteStepAsync(sagaInstance, nextStep);
                }
                else
                {
                    _logger.LogWarning($"Step {stepName} not found in flow {sagaInstance.FlowName} for saga {sagaInstance.SagaId}");
                }
            //}
        }

        private async Task StartNewFlowAsync(SagaInstance sagaInstance, string flowName)
        {
            _logger.LogInformation($"Starting new flow {flowName} for saga {sagaInstance.SagaId}");

            var allFlows = _flowDefinition.GetAllFlows();
            if (!allFlows.ContainsKey(flowName))
            {
                _logger.LogError($"Flow {flowName} not found");
                sagaInstance.CompleteSaga(false);
                return;
            }

            var flow = allFlows[flowName];
            var firstStep = flow.Steps.FirstOrDefault();

            if (firstStep == null)
            {
                _logger.LogError($"Flow {flowName} has no steps");
                sagaInstance.CompleteSaga(false);
                return;
            }

            // Update saga instance for the new flow
            sagaInstance.FlowName = flowName;
            sagaInstance.CurrentStep = firstStep.Name;

            // Set appropriate status based on flow type
            if (_flowDefinition.IsRollbackFlow(flowName))
            {
                sagaInstance.StartRollback();
            }

            await ExecuteStepAsync(sagaInstance, firstStep);
        }

        private async Task CheckSagaCompletionAsync(SagaInstance sagaInstance)
        {
            var allFlows = _flowDefinition.GetAllFlows();
            var flow = allFlows[sagaInstance.FlowName];
            
            // Check if all steps are completed
            var hasUncompletedSteps = flow.Steps.Any(step => 
            {
                var stepExecution = sagaInstance.GetStep(step.Name);
                return stepExecution == null || stepExecution.Status == StepExecutionStatus.RUNNING;
            });
            
            if (!hasUncompletedSteps)
            {
                var completionStatus = _flowDefinition.IsRollbackFlow(sagaInstance.FlowName) 
                    ? SagaStatus.RolledBack 
                    : SagaStatus.Completed;
                
                sagaInstance.CompleteSaga(completionStatus == SagaStatus.Completed);
                _logger.LogInformation($"Saga {sagaInstance.SagaId} completed with status {completionStatus}");
            }
        }

        // Update existing ExecuteStepAsync method
        private async Task ExecuteStepAsync(SagaInstance sagaInstance, StepDefinition step)
        {
            _logger.LogInformation($"Executing step {step.Name} for saga {sagaInstance.SagaId}");

            sagaInstance.StartStep(step.Name);
            sagaInstance.CurrentStep = step.Name;

            var allFlows = _flowDefinition.GetAllFlows();
            var flow = allFlows[sagaInstance.FlowName];
            var currentStep = flow.Steps.FirstOrDefault(s => s.Name == step.Name);
            var OnSuccessNextFlow = currentStep?.OnSuccess?.NextFlows;
            var OnFailureNextFlow = currentStep?.OnFailure?.NextFlows;

            var command = new SagaCommand
            {
                SagaId = sagaInstance.SagaId,
                CommandType = step.Command,
                StepName = step.Name,
                FlowName = sagaInstance.FlowName,
                OnSuccessNextFlow = OnSuccessNextFlow,
                OnFailureNextFlow = OnFailureNextFlow,
                Data = sagaInstance.Context,
                Timestamp = DateTime.UtcNow
            };

            var commandJson = JsonSerializer.Serialize(command);
            var topic = GetTopicForCommand(step.Command);

            await _producer.ProduceAsync(topic, new Message<string, string>
            {
                Key = sagaInstance.SagaId.ToString(),
                Value = commandJson
            });

            _logger.LogInformation($"Published command {step.Command} to topic {topic} for saga {sagaInstance.SagaId}");
        }

        private string GetTopicForCommand(string command)
        {
            // Map commands to appropriate topics based on naming convention
            if (command.Contains("verify-user") && command.Contains(".command"))
                return "user-command";

            // Order-related commands
            if (command.Contains("create-order") && command.Contains(".command"))
                return "order-command";

            // Inventory/Product-related commands
            if (command.Contains("reserve-inventory") && command.Contains(".command"))
                return "product-command";

            // Payment-related commands
            if (command.Contains("process-payment") && command.Contains(".command"))
                return "payment-command";

            // Notification/Email-related commands
            if (command.Contains("send-notification") && command.Contains(".command"))
                return "notification-command";

            return "saga-orchestration";
        }

        public async Task<SagaInstance?> GetSagaInstance(Guid sagaId)
        {
            return await _sagaRepository.GetSagaInstanceAsync(sagaId);
        }

        public async Task<IEnumerable<SagaInstance>> GetAllActiveSagas()
        {
            return await _sagaRepository.GetActiveSagasAsync();
        }

        // Helper method to add new properties only
        private void AddNewPropertiesOnly(Dictionary<string, object> target, Dictionary<string, object>? source)
        {
            if (source == null) return;

            foreach (var kvp in source)
            {
                if (!target.ContainsKey(kvp.Key))
                {
                    target[kvp.Key] = kvp.Value;
                    _logger.LogDebug($"Added new property '{kvp.Key}' to context");
                }
            }
        }
    }
}
