using System.ComponentModel.DataAnnotations;
using System.Text.Json;

namespace SagaOrchestratorService.Models
{
    public class SagaInstanceEntity
    {
        public Guid SagaId { get; set; }
        public string FlowName { get; set; } = string.Empty;
        public string CurrentStep { get; set; } = string.Empty;
        public SagaStatus Status { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        public string ContextJson { get; set; } = "{}";
        
        // Concurrency token for optimistic locking
        [Timestamp]
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();
        
        // Navigation property
        public List<SagaStepExecutionEntity> Steps { get; set; } = new();
        
        // Convert to domain model
        public SagaInstance ToSagaInstance()
        {
            var sagaInstance = new SagaInstance
            {
                SagaId = SagaId,
                FlowName = FlowName,
                CurrentStep = CurrentStep,
                Status = Status,
                StartTime = StartTime,
                EndTime = EndTime,
                Context = string.IsNullOrEmpty(ContextJson) 
                    ? new Dictionary<string, object>() 
                    : JsonSerializer.Deserialize<Dictionary<string, object>>(ContextJson) ?? new Dictionary<string, object>(),
                Steps = Steps.Select(s => s.ToSagaStepExecution()).ToList()
            };
            
            return sagaInstance;
        }
        
        // Create from domain model
        public static SagaInstanceEntity FromSagaInstance(SagaInstance sagaInstance)
        {
            return new SagaInstanceEntity
            {
                SagaId = sagaInstance.SagaId,
                FlowName = sagaInstance.FlowName,
                CurrentStep = sagaInstance.CurrentStep,
                Status = sagaInstance.Status,
                StartTime = sagaInstance.StartTime,
                EndTime = sagaInstance.EndTime,
                ContextJson = JsonSerializer.Serialize(sagaInstance.Context),
                Steps = sagaInstance.Steps.Select(s => SagaStepExecutionEntity.FromSagaStepExecution(s, sagaInstance.SagaId)).ToList()
            };
        }
        
        // Update from domain model
        public void UpdateFromSagaInstance(SagaInstance sagaInstance)
        {
            FlowName = sagaInstance.FlowName;
            CurrentStep = sagaInstance.CurrentStep;
            Status = sagaInstance.Status;
            EndTime = sagaInstance.EndTime;
            ContextJson = JsonSerializer.Serialize(sagaInstance.Context);
        }
    }

    public class SagaStepExecutionEntity
    {
        public int Id { get; set; }
        public Guid SagaId { get; set; }
        public string Step { get; set; } = string.Empty;
        public string Service { get; set; } = string.Empty;
        public string Action { get; set; } = string.Empty;
        public StepExecutionStatus Status { get; set; }
        public DateTime StartedAt { get; set; }
        public DateTime? EndedAt { get; set; }
        public string? ErrorMessage { get; set; }
        public string? ResultJson { get; set; }
        
        // Navigation property
        public SagaInstanceEntity SagaInstance { get; set; } = null!;
        
        // Convert to domain model
        public SagaStepExecution ToSagaStepExecution()
        {
            return new SagaStepExecution
            {
                Step = Step,
                Service = Service,
                Action = Action,
                Status = Status,
                StartedAt = StartedAt,
                EndedAt = EndedAt,
                ErrorMessage = ErrorMessage,
                Result = string.IsNullOrEmpty(ResultJson) 
                    ? new Dictionary<string, object>() 
                    : JsonSerializer.Deserialize<Dictionary<string, object>>(ResultJson) ?? new Dictionary<string, object>()
            };
        }
        
        // Create from domain model
        public static SagaStepExecutionEntity FromSagaStepExecution(SagaStepExecution step, Guid sagaId)
        {
            return new SagaStepExecutionEntity
            {
                SagaId = sagaId,
                Step = step.Step,
                Service = step.Service,
                Action = step.Action,
                Status = step.Status,
                StartedAt = step.StartedAt,
                EndedAt = step.EndedAt,
                ErrorMessage = step.ErrorMessage,
                ResultJson = step.Result.Any() ? JsonSerializer.Serialize(step.Result) : null
            };
        }
    }
}
