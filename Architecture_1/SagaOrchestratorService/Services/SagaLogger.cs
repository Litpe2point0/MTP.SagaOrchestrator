using Microsoft.Extensions.Logging;

namespace SagaOrchestratorService.Services
{
    public class SagaLogger : ISagaLogger
    {
        private readonly ILogger<SagaLogger> _logger;

        public SagaLogger(ILogger<SagaLogger> logger)
        {
            _logger = logger;
        }

        public void LogSagaStart(Guid sagaId, string flowName)
        {
            _logger.LogInformation("?? SAGA START | ID: {SagaId} | Flow: {FlowName}", 
                sagaId.ToString()[..8], flowName);
        }

        public void LogStepExecution(Guid sagaId, string stepName, string status)
        {
            var icon = status switch
            {
                "EXECUTING" => "?",
                "WAITING" => "?",
                "SKIPPED" => "??",
                "IMMEDIATE" => "??",
                _ => "??"
            };
            
            _logger.LogInformation("{Icon} STEP {Status} | Saga: {SagaId} | Step: {StepName}", 
                icon, status, sagaId.ToString()[..8], stepName);
        }

        public void LogStepCompletion(Guid sagaId, string stepName, bool success, string? error = null)
        {
            if (success)
            {
                _logger.LogInformation("? STEP SUCCESS | Saga: {SagaId} | Step: {StepName}", 
                    sagaId.ToString()[..8], stepName);
            }
            else
            {
                _logger.LogError("? STEP FAILED | Saga: {SagaId} | Step: {StepName} | Error: {Error}", 
                    sagaId.ToString()[..8], stepName, error ?? "Unknown error");
            }
        }

        public void LogSagaCompletion(Guid sagaId, string status)
        {
            var icon = status switch
            {
                "Completed" => "??",
                "Failed" => "??",
                "RolledBack" => "??",
                _ => "??"
            };
            
            _logger.LogInformation("{Icon} SAGA {Status} | ID: {SagaId}", 
                icon, status.ToUpper(), sagaId.ToString()[..8]);
        }

        public void LogDependencyCheck(Guid sagaId, string stepName, List<string> dependencies)
        {
            _logger.LogInformation("?? DEPENDENCY CHECK | Saga: {SagaId} | Step: {StepName} | Waiting for: [{Dependencies}]", 
                sagaId.ToString()[..8], stepName, string.Join(", ", dependencies));
        }

        public void LogFlowTransition(Guid sagaId, string fromFlow, string toFlow)
        {
            _logger.LogInformation("?? FLOW TRANSITION | Saga: {SagaId} | From: {FromFlow} | To: {ToFlow}", 
                sagaId.ToString()[..8], fromFlow, toFlow);
        }
    }
}