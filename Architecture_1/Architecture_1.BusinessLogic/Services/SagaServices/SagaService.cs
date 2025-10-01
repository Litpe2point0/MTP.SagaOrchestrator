using Architecture_1.BusinessLogic.Services.SagaServices.interfaces;
using Architecture_1.DataAccess.Entities;
using Architecture_1.DataAccess.Repositories.interfaces;
using Microsoft.Extensions.Logging;

namespace Architecture_1.BusinessLogic.Services.SagaServices
{
    public class SagaService : ISagaService
    {
        private readonly IGenericRepository<SagaInstance> _sagaInstanceRepository;
        private readonly IGenericRepository<SagaStepExcecution> _stepExecutionRepository;
        private readonly ILogger<SagaService> _logger;

        public SagaService(
            IGenericRepository<SagaInstance> sagaInstanceRepository,
            IGenericRepository<SagaStepExcecution> stepExecutionRepository,
            ILogger<SagaService> logger)
        {
            _sagaInstanceRepository = sagaInstanceRepository;
            _stepExecutionRepository = stepExecutionRepository;
            _logger = logger;
        }

        public async Task<SagaInstance> CreateSagaInstanceAsync(Guid sagaId, string flowName, Dictionary<string, object> initialData, string firstStepName)
        {
            var sagaInstance = new SagaInstance
            {
                SagaId = sagaId,
                FlowName = flowName,
                CurrentStepName = firstStepName,
                InitialData = initialData,
                ResultData = new Dictionary<string, object>(),
                FlowStatus = SagaStatus.RUNNING,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            var created = await _sagaInstanceRepository.CreateAsync(sagaInstance);
            _logger.LogInformation("Created saga instance: {SagaId}, Flow: {FlowName}", sagaId, flowName);
            
            return created ?? sagaInstance;
        }

        public async Task<SagaStepExcecution> CreateStepExecutionAsync(Guid sagaId, string stepName, string? topicName, Dictionary<string, object>? requestData)
        {
            var stepExecution = new SagaStepExcecution
            {
                SagaId = sagaId,
                StepName = stepName,
                TopicName = topicName,
                StepStatus = StepStatus.RUNNING,
                RequestData = requestData ?? new Dictionary<string, object>(),
                responseData = new Dictionary<string, object>(),
                CreatedAt = DateTime.UtcNow
            };

            var created = await _stepExecutionRepository.CreateAsync(stepExecution);
            _logger.LogInformation("Created step execution: {SagaId}, Step: {StepName}", sagaId, stepName);
            
            return created ?? stepExecution;
        }

        public async Task UpdateStepExecutionStatusAsync(Guid sagaId, string stepName, StepStatus status, Dictionary<string, object>? responseData = null, string? errorMessage = null)
        {
            var stepExecutions = await GetStepExecutionsAsync(sagaId);
            var stepExecution = stepExecutions.FirstOrDefault(s => s.StepName == stepName && s.StepStatus == StepStatus.RUNNING);

            if (stepExecution != null)
            {
                stepExecution.StepStatus = status;
                if (responseData != null)
                    stepExecution.responseData = responseData;
                if (!string.IsNullOrWhiteSpace(errorMessage))
                    stepExecution.ErrorMessage = errorMessage;

                await _stepExecutionRepository.UpdateAsync(stepExecution.Id, stepExecution);
                _logger.LogInformation("Updated step execution: {SagaId}, Step: {StepName}, Status: {Status}", sagaId, stepName, status);
            }
        }

        public async Task UpdateSagaCurrentStepAsync(Guid sagaId, string? currentStepName)
        {
            var sagaInstance = await GetSagaInstanceAsync(sagaId);
            if (sagaInstance != null)
            {
                sagaInstance.CurrentStepName = currentStepName;
                sagaInstance.UpdatedAt = DateTime.UtcNow;
                await _sagaInstanceRepository.UpdateAsync(sagaInstance.SagaId, sagaInstance);
                _logger.LogInformation("Updated saga current step: {SagaId}, Step: {StepName}", sagaId, currentStepName);
            }
        }

        public async Task<bool> CheckAndUpdateSagaCompletionAsync(Guid sagaId)
        {
            var stepExecutions = await GetStepExecutionsAsync(sagaId);
            
            // Check if all steps are completed successfully
            var allSuccess = stepExecutions.All(s => s.StepStatus == StepStatus.SUCCESS);
            var anyRunning = stepExecutions.Any(s => s.StepStatus == StepStatus.RUNNING);

            if (allSuccess && !anyRunning)
            {
                await UpdateSagaStatusAsync(sagaId, SagaStatus.SUCCESS);
                _logger.LogInformation("Saga completed successfully: {SagaId}", sagaId);
                return true;
            }

            return false;
        }

        public async Task UpdateSagaStatusAsync(Guid sagaId, SagaStatus status, Dictionary<string, object>? resultData = null, string? errorStepName = null, string? errorMessage = null)
        {
            var sagaInstance = await GetSagaInstanceAsync(sagaId);
            if (sagaInstance != null)
            {
                sagaInstance.FlowStatus = status;
                sagaInstance.UpdatedAt = DateTime.UtcNow;

                if (resultData != null)
                    sagaInstance.ResultData = resultData;
                
                if (!string.IsNullOrWhiteSpace(errorStepName))
                    sagaInstance.ErrorStepName = errorStepName;
                
                if (!string.IsNullOrWhiteSpace(errorMessage))
                    sagaInstance.ErrorMessage = errorMessage;

                if (status == SagaStatus.SUCCESS || status == SagaStatus.FAILED)
                    sagaInstance.CompletedAt = DateTime.UtcNow;

                await _sagaInstanceRepository.UpdateAsync(sagaInstance.SagaId, sagaInstance);
                _logger.LogInformation("Updated saga status: {SagaId}, Status: {Status}", sagaId, status);
            }
        }

        public async Task<SagaInstance?> GetSagaInstanceAsync(Guid sagaId)
        {
            return await _sagaInstanceRepository.FindByIdAsync(sagaId);
        }

        public async Task<List<SagaStepExcecution>> GetStepExecutionsAsync(Guid sagaId)
        {
            var allExecutions = _stepExecutionRepository.FindAll();
            return allExecutions.Where(s => s.SagaId == sagaId).ToList();
        }
    }
}