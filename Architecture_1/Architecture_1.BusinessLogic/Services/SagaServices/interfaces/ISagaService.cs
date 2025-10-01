using Architecture_1.DataAccess.Entities;

namespace Architecture_1.BusinessLogic.Services.SagaServices.interfaces
{
    public interface ISagaService
    {
        Task<SagaInstance> CreateSagaInstanceAsync(Guid sagaId, string flowName, Dictionary<string, object> initialData, string firstStepName);
        Task<SagaStepExcecution> CreateStepExecutionAsync(Guid sagaId, string stepName, string? topicName, Dictionary<string, object>? requestData);
        Task UpdateStepExecutionStatusAsync(Guid sagaId, string stepName, StepStatus status, Dictionary<string, object>? responseData = null, string? errorMessage = null);
        Task UpdateSagaCurrentStepAsync(Guid sagaId, string? currentStepName);
        Task<bool> CheckAndUpdateSagaCompletionAsync(Guid sagaId);
        Task UpdateSagaStatusAsync(Guid sagaId, SagaStatus status, Dictionary<string, object>? resultData = null, string? errorStepName = null, string? errorMessage = null);
        Task<SagaInstance?> GetSagaInstanceAsync(Guid sagaId);
        Task<List<SagaStepExcecution>> GetStepExecutionsAsync(Guid sagaId);
    }
}