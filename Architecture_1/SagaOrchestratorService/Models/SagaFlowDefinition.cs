using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace SagaOrchestratorService.Models
{
    public class SagaFlowDefinition
    {
        public string Version { get; set; } = string.Empty;

        // Single flows property instead of separate forward/rollback
        public Dictionary<string, FlowDefinition> Flows { get; set; } = new();

        // Simplified method to get all flows
        public Dictionary<string, FlowDefinition> GetAllFlows()
        {
            return new Dictionary<string, FlowDefinition>(Flows);
        }

        // Helper method to check if a flow is a rollback flow
        public bool IsRollbackFlow(string flowName)
        {
            return flowName.Contains("rollback", StringComparison.OrdinalIgnoreCase);
        }
    }

    public class FlowDefinition
    {
        public string Description { get; set; } = string.Empty;
        public List<StepDefinition> Steps { get; set; } = new();
        public bool IsRollbackFlow { get; set; }
    }

    public class StepDefinition
    {
        public string Name { get; set; } = string.Empty;
        public string Command { get; set; } = string.Empty;

        [YamlMember(Alias = "fireAndForget", ApplyNamingConventions = false)]
        public List<string> FireAndForget { get; set; } = new();

        [YamlMember(Alias = "onSuccess", ApplyNamingConventions = false)]
        public StepOutcome? OnSuccess { get; set; }

        [YamlMember(Alias = "onFailure", ApplyNamingConventions = false)]
        public StepOutcome? OnFailure { get; set; }
    }

    public class StepOutcome
    {
        public string? Emit { get; set; }

        [YamlMember(Alias = "nextSteps", ApplyNamingConventions = false)]
        public object? NextStepsRaw { get; set; } // Accept either string or list

        [YamlMember(Alias = "nextFlows", ApplyNamingConventions = false)]
        public string? NextFlows { get; set; }

        // Computed property to always return List<string>
        [YamlIgnore]
        public List<string>? NextSteps
        {
            get
            {
                if (NextStepsRaw == null) return null;

                if (NextStepsRaw is string singleStep)
                {
                    return new List<string> { singleStep };
                }

                if (NextStepsRaw is List<object> steps)
                {
                    return steps.Select(s => s.ToString()).Where(s => !string.IsNullOrEmpty(s)).ToList();
                }

                return null;
            }
        }

        // Helper method to check if this outcome triggers rollback steps
        public bool HasRollbackSteps => NextSteps?.Any(step => step.Contains(".rollback")) ?? false;

        // Helper method to get rollback steps
        public List<string> GetRollbackSteps() => NextSteps?.Where(step => step.Contains(".rollback")).ToList() ?? new List<string>();

        // Helper method to get regular (non-rollback) steps
        public List<string> GetRegularSteps() => NextSteps?.Where(step => !step.Contains(".rollback")).ToList() ?? new List<string>();
    }

    // Keep other classes unchanged...
    public class SagaInstance
    {
        public Guid SagaId { get; set; } = Guid.NewGuid();
        public string FlowName { get; set; } = string.Empty;
        public string CurrentStep { get; set; } = string.Empty;
        public SagaStatus Status { get; set; } = SagaStatus.Running;
        public DateTime StartTime { get; set; } = DateTime.UtcNow;
        public DateTime? EndTime { get; set; }
        public Dictionary<string, object> Context { get; set; } = new();
        public List<SagaStepExecution> Steps { get; set; } = new();

        // Helper methods for step management
        public void StartStep(string stepName, string? service = null, string? action = null)
        {
            var step = new SagaStepExecution
            {
                Step = stepName,
                Service = service ?? ExtractServiceFromStep(stepName),
                Action = action ?? ExtractActionFromStep(stepName),
                Status = StepExecutionStatus.RUNNING,
                StartedAt = DateTime.UtcNow,
            };

            Steps.Add(step);
        }

        public void CompleteStep(string stepName, bool isSuccess, string? errorMessage = null, Dictionary<string, object>? result = null)
        {
            var step = Steps.FirstOrDefault(s => s.Step == stepName);
            if (step != null)
            {
                step.Status = isSuccess ? StepExecutionStatus.SUCCESS : StepExecutionStatus.FAILED;
                step.EndedAt = DateTime.UtcNow;
                step.ErrorMessage = errorMessage;
                if (result != null)
                {
                    step.Result = result;
                }
            }
        }

        public void SkipStep(string stepName, string reason)
        {
            var step = Steps.FirstOrDefault(s => s.Step == stepName);
            if (step != null)
            {
                step.Status = StepExecutionStatus.SKIPPED;
                step.EndedAt = DateTime.UtcNow;
                step.ErrorMessage = reason;
            }
        }

        public void MarkStepAsRolledBack(string stepName)
        {
            var step = Steps.FirstOrDefault(s => s.Step == stepName);
            if (step != null)
            {
                step.Status = StepExecutionStatus.ROLLED_BACK;
                step.EndedAt = DateTime.UtcNow;
            }
        }

        // Query helpers
        public List<SagaStepExecution> GetCompletedSteps() =>
            Steps.Where(s => s.Status == StepExecutionStatus.SUCCESS).ToList();

        public List<SagaStepExecution> GetFailedSteps() =>
            Steps.Where(s => s.Status == StepExecutionStatus.FAILED).ToList();

        public List<SagaStepExecution> GetRunningSteps() =>
            Steps.Where(s => s.Status == StepExecutionStatus.RUNNING).ToList();

        public List<SagaStepExecution> GetRolledBackSteps() =>
            Steps.Where(s => s.Status == StepExecutionStatus.ROLLED_BACK).ToList();

        public SagaStepExecution? GetStep(string stepName) =>
            Steps.FirstOrDefault(s => s.Step == stepName);

        public bool IsStepCompleted(string stepName) =>
            Steps.Any(s => s.Step == stepName && s.Status == StepExecutionStatus.SUCCESS);

        public bool IsStepFailed(string stepName) =>
            Steps.Any(s => s.Step == stepName && s.Status == StepExecutionStatus.FAILED);

        public bool IsStepRunning(string stepName) =>
            Steps.Any(s => s.Step == stepName && s.Status == StepExecutionStatus.RUNNING);

        // Completion management
        public void CompleteSaga(bool isSuccess)
        {
            Status = isSuccess ? SagaStatus.Completed : SagaStatus.Failed;
            EndTime = DateTime.UtcNow;
        }

        public void StartRollback()
        {
            Status = SagaStatus.RollingBack;
        }

        public void CompleteRollback()
        {
            Status = SagaStatus.RolledBack;
            EndTime = DateTime.UtcNow;
        }

        // Computed properties
        public TimeSpan? ExecutionTime => EndTime.HasValue ? EndTime.Value - StartTime : DateTime.UtcNow - StartTime;
        public int TotalSteps => Steps.Count;
        public int CompletedStepsCount => GetCompletedSteps().Count;
        public int FailedStepsCount => GetFailedSteps().Count;
        public int RunningStepsCount => GetRunningSteps().Count;
        public bool HasFailures => GetFailedSteps().Any();
        public bool AllStepsCompleted => Steps.All(s => s.Status != StepExecutionStatus.RUNNING && s.Status != StepExecutionStatus.PENDING);

        // Helper methods for extracting service/action from step name
        private string ExtractServiceFromStep(string stepName)
        {
            if (stepName.Contains("order"))
                return "OrderService";
            if (stepName.Contains("inventory") || stepName.Contains("product"))
                return "ProductService";
            if (stepName.Contains("user"))
                return "UserService";
            if (stepName.Contains("payment"))
                return "PaymentService";
            if (stepName.Contains("email") || stepName.Contains("notification"))
                return "NotificationService";

            return "UnknownService";
        }

        private string ExtractActionFromStep(string stepName)
        {
            if (stepName.StartsWith("create"))
                return "create";
            if (stepName.StartsWith("reserve"))
                return "reserve";
            if (stepName.StartsWith("rollback"))
                return "rollback";
            if (stepName.StartsWith("send"))
                return "send";
            if (stepName.StartsWith("process"))
                return "process";

            return stepName.Split('-')[0];
        }
    }

    public class SagaStepExecution
    {
        public string Step { get; set; } = string.Empty;
        public string Service { get; set; } = string.Empty;
        public string Action { get; set; } = string.Empty;
        public StepExecutionStatus Status { get; set; } = StepExecutionStatus.PENDING;
        public DateTime StartedAt { get; set; } = DateTime.UtcNow;
        public DateTime? EndedAt { get; set; }
        public string? ErrorMessage { get; set; }
        public Dictionary<string, object> Result { get; set; } = new();
        
        public TimeSpan? ExecutionTime => EndedAt.HasValue ? EndedAt.Value - StartedAt : null;
        public bool IsCompleted => Status == StepExecutionStatus.SUCCESS || Status == StepExecutionStatus.FAILED || Status == StepExecutionStatus.SKIPPED;
        public bool IsRunning => Status == StepExecutionStatus.RUNNING;
        public bool IsSuccessful => Status == StepExecutionStatus.SUCCESS;
        public bool HasFailed => Status == StepExecutionStatus.FAILED;
    }

    public enum StepExecutionStatus
    {
        PENDING,
        RUNNING,
        SUCCESS,
        FAILED,
        SKIPPED,
        ROLLED_BACK
    }

    public enum SagaStatus
    {
        Running,
        Completed,
        Failed,
        RollingBack,
        RolledBack
    }

    public class SagaCommand
    {
        public Guid SagaId { get; set; }
        public string CommandType { get; set; } = string.Empty;
        public string StepName { get; set; } = string.Empty;
        public string FlowName { get; set; } = string.Empty;
        public string OnSuccessNextFlow { get; set; } = string.Empty;
        public string OnFailureNextFlow { get; set; } = string.Empty;
        public Dictionary<string, object> Data { get; set; } = new();
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }

    public class SagaEvent
    {
        public Guid SagaId { get; set; }
        public string EventType { get; set; } = string.Empty;
        public string StepName { get; set; } = string.Empty;
        public string FlowName { get; set; } = string.Empty;
        public Dictionary<string, object> Data { get; set; } = new();
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public bool IsSuccess { get; set; }
        public string? ErrorMessage { get; set; }
    }

    public static class SagaFlowParser
    {
        public static SagaFlowDefinition ParseFromFile(string filePath)
        {
            var yaml = File.ReadAllText(filePath);
            Console.WriteLine($"Parsing YAML from file: {yaml}");
            return ParseFromYaml(yaml);
        }

        public static SagaFlowDefinition ParseFromYaml(string yaml)
        {
            var deserializer = new DeserializerBuilder()
                .WithNamingConvention(CamelCaseNamingConvention.Instance)
                .Build();
            var result = deserializer.Deserialize<SagaFlowDefinition>(yaml);

            // Log flows
            foreach (var flow in result.Flows)
            {
                Console.WriteLine($"Flow: {flow.Key}, Description: {flow.Value.Description}");
                foreach (var step in flow.Value.Steps)
                {
                    Console.WriteLine($"  Step: {step.Name}, Command: {step.Command}");
                    if (step.OnSuccess != null)
                    {
                        Console.WriteLine($"    OnSuccess NextSteps: {(step.OnSuccess.NextSteps == null ? "None" : string.Join(", ", step.OnSuccess.NextSteps))}, NextFlows: {step.OnSuccess.NextFlows ?? "None"}, Emit: {step.OnSuccess.Emit ?? "None"}");
                    }
                    if (step.OnFailure != null)
                    {
                        Console.WriteLine($"    OnFailure NextSteps: {(step.OnFailure.NextSteps == null ? "None" : string.Join(", ", step.OnFailure.NextSteps))}, NextFlows: {step.OnFailure.NextFlows ?? "None"}, Emit: {step.OnFailure.Emit ?? "None"}");
                    }
                }
            }

            return result;
        }
    }
}
