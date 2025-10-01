using SagaOrchestratorService.Models;

namespace SagaOrchestratorService.DTOs
{
    public class SagaInstanceDto
    {
        public string SagaId { get; set; } = string.Empty;
        public string FlowId { get; set; } = string.Empty;
        public SagaStatusDto Status { get; set; } = SagaStatusDto.Started;
        public string CurrentStep { get; set; } = string.Empty;
        public List<string> CompletedSteps { get; set; } = new();
        public List<string> FailedSteps { get; set; } = new();
        public List<string> CompensatedSteps { get; set; } = new();
        public Dictionary<string, object> Context { get; set; } = new();
        public DateTime StartedAt { get; set; } = DateTime.UtcNow;
        public DateTime? CompletedAt { get; set; }
        public string? ErrorMessage { get; set; }
        public int RetryCount { get; set; } = 0;
    }

    public enum SagaStatusDto
    {
        Started,
        InProgress,
        Completed,
        Failed,
        Compensating,
        Compensated
    }

    public class SagaCommandMessageDto
    {
        public string SagaId { get; set; } = string.Empty;
        public string CommandType { get; set; } = string.Empty;
        public string SourceService { get; set; } = "SagaOrchestrator";
        public string TargetService { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public string Step { get; set; } = string.Empty;
        public Dictionary<string, string> Payload { get; set; } = new();
    }

    public class SagaResponseMessageDto
    {
        public string SagaId { get; set; } = string.Empty;
        public string EventType { get; set; } = string.Empty;
        public string Step { get; set; } = string.Empty;
        public string SourceService { get; set; } = string.Empty;
        public bool Success { get; set; }
        public string? ErrorMessage { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public Dictionary<string, string> Payload { get; set; } = new();
    }

    public class SagaEventDto
    {
        public string SagaId { get; set; } = string.Empty;
        public string EventType { get; set; } = string.Empty;
        public string Function { get; set; } = string.Empty;
        public string SourceService { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public Dictionary<string, string> Data { get; set; } = new();
    }

    public class StartSagaRequestDto
    {
        public string FlowId { get; set; } = string.Empty;
        public Dictionary<string, object> InitialData { get; set; } = new();
    }
}
