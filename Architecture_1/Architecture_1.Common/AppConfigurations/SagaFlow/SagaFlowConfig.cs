using Architecture_1.Common.AppConfigurations.SagaFlow.interfaces;
using Microsoft.Extensions.Configuration;
using System.IO;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Architecture_1.Common.AppConfigurations.SagaFlow
{
    internal class SagaFlowFileModel
    {
        public string Version { get; set; } = "1.0";
        public Dictionary<string, SagaFlowDefinitionModel> Flows { get; set; } = new();
    }

    public class SagaFlowConfig : ISagaFlowConfig
    {
        public bool Loaded { get; private set; }
        public string Version { get; private set; } = "1.0";
        public IReadOnlyDictionary<string, SagaFlowDefinitionModel> Flows => _flows;
        private readonly Dictionary<string, SagaFlowDefinitionModel> _flows = new(StringComparer.OrdinalIgnoreCase);

        public SagaFlowConfig(IConfiguration configuration)
        {
            // Prefer path from configuration. Example in appsettings.json:
            // "Flow": { "YAML": { "Path": "SagaOrchestratorService/SagaFlows/order-processing-flow-new.yaml" } }
            var configuredPath = configuration["Flow:YAML:Path"];

            configuredPath = configuredPath.Replace("\\", Path.DirectorySeparatorChar.ToString());
            string basePath = AppDomain.CurrentDomain.BaseDirectory.TrimEnd(Path.DirectorySeparatorChar);
            string relativePath = configuredPath.TrimStart(Path.DirectorySeparatorChar);

            string yamlPath = Path.Combine(basePath, relativePath);

            Console.WriteLine($"Loading SagaFlowConfig from: {yamlPath}");
            //string yamlPath = !string.IsNullOrWhiteSpace(configuredPath)
            //    ? Path.Combine(AppContext.BaseDirectory, configuredPath)
            //    : Path.Combine(AppContext.BaseDirectory, "AppConfigurations", "SagaFlow", "order-processing-flow-new.yaml");

            if (!File.Exists(yamlPath))
            {
                Console.WriteLine($"Warning: YAML flow definition not found at: {yamlPath}");
                // Keep defaults; Loaded stays false, but service is alive to avoid startup failure
                return;
            }

            try
            {
                var yaml = File.ReadAllText(yamlPath);
                var deserializer = new DeserializerBuilder()
                    .WithNamingConvention(CamelCaseNamingConvention.Instance)
                    .IgnoreUnmatchedProperties()
                    .Build();

                var file = deserializer.Deserialize<SagaFlowFileModel>(yaml) ?? new SagaFlowFileModel();
                Version = string.IsNullOrWhiteSpace(file.Version) ? "1.0" : file.Version;

                _flows.Clear();
                foreach (var kv in file.Flows)
                {
                    _flows[kv.Key] = kv.Value ?? new SagaFlowDefinitionModel();
                }

                foreach (var flow in file.Flows)
                {
                    Console.WriteLine($"Flow: {flow.Key}, Description: {flow.Value.Description}");
                    foreach (var step in flow.Value.Steps)
                    {
                        Console.WriteLine($"  Step: {step.Name}, Topic: {step.Topic}");
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
                Loaded = true;
                Console.WriteLine($"Successfully loaded YAML flow definition from: {yamlPath}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading YAML: {ex.Message}");
                // Fallback stays as empty/default (avoid throwing to keep app up)
                Loaded = false;
            }
        }
    }
}