namespace SagaOrchestratorService.Services
{
    public interface ISagaLogger
    {
        void LogSagaStart(Guid sagaId, string flowName);
        void LogStepExecution(Guid sagaId, string stepName, string status);
        void LogStepCompletion(Guid sagaId, string stepName, bool success, string? error = null);
        void LogSagaCompletion(Guid sagaId, string status);
        void LogDependencyCheck(Guid sagaId, string stepName, List<string> dependencies);
        void LogFlowTransition(Guid sagaId, string fromFlow, string toFlow);
    }
}