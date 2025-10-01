using System.Collections.Generic;

namespace Architecture_1.Common.AppConfigurations.SagaFlow.interfaces
{
    public interface ISagaFlowConfig
    {
        bool Loaded { get; }
        string Version { get; }
        IReadOnlyDictionary<string, SagaFlowDefinitionModel> Flows { get; }
    }

    public class SagaFlowDefinitionModel
    {
        public string Topic { get; set; } = string.Empty;
        public string? Description { get; set; }
        public List<SagaStepDefinitionModel> Steps { get; set; } = new();
    }

    public class SagaStepDefinitionModel
    {
        public string Name { get; set; } = string.Empty;
        public string Topic { get; set; } = string.Empty;
        public SagaOutcomeDefinitionModel? OnSuccess { get; set; }
        public SagaOutcomeDefinitionModel? OnFailure { get; set; }
    }

    public class SagaOutcomeDefinitionModel
    {
        public string Emit { get; set; } = string.Empty;
        public string Topic { get; set; } = string.Empty;
        public List<SagaStepRefModel> NextSteps { get; set; } = new();

        // Changed: single next flow path (string). YAML key remains "nextFlows".
        public string? NextFlows { get; set; } // keep YAML alias name for compatibility

        // Convenience accessor (single path only)
        public string? NextFlow => string.IsNullOrWhiteSpace(NextFlows) ? null : NextFlows;
    }

    public class SagaStepRefModel
    {
        public string Name { get; set; } = string.Empty;
        public string Topic { get; set; } = string.Empty;
    }
}