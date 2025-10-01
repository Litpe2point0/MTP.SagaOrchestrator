using Confluent.Kafka;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SagaOrchestratorService.Models;
using System.Text.Json;

namespace SagaOrchestratorService.Services
{
    public class SagaOrchestratorBackgroundService : BackgroundService
    {
        private readonly ILogger<SagaOrchestratorBackgroundService> _logger;
        private readonly IConfiguration _configuration;
        private readonly IServiceScopeFactory _serviceScopeFactory;

        public SagaOrchestratorBackgroundService(
            ILogger<SagaOrchestratorBackgroundService> logger, 
            IConfiguration configuration,
            IServiceScopeFactory serviceScopeFactory)
        {
            _logger = logger;
            _configuration = configuration;
            _serviceScopeFactory = serviceScopeFactory;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            // Add initial delay to let the web server start first
            await Task.Delay(2000, stoppingToken);
            
            _logger.LogInformation("Starting Saga Orchestrator Background Service...");

            var consumerConfig = new ConsumerConfig
            {
                BootstrapServers = _configuration["Kafka:BootstrapServers"] ?? "172.26.128.1:9092",
                GroupId = "saga-orchestrator-group",
                AutoOffsetReset = AutoOffsetReset.Latest,
                EnableAutoCommit = true,
                SessionTimeoutMs = 10000,
                MaxPollIntervalMs = 300000,
                HeartbeatIntervalMs = 3000,
                MetadataMaxAgeMs = 30000
            };

            IConsumer<string, string>? consumer = null;

            try
            {
                consumer = new ConsumerBuilder<string, string>(consumerConfig)
                    .SetErrorHandler((_, e) => _logger.LogError($"Kafka Consumer Error: {e.Reason}"))
                    .SetLogHandler((_, log) => _logger.LogDebug($"Kafka Consumer Log: {log.Message}"))
                    .Build();

                var topics = new[]
                {
                    "saga-orchestration",
                    "order-event",
                    "product-event",
                    "payment-event",
                    "user-event"
                };

                consumer.Subscribe(topics);
                _logger.LogInformation($"Saga Orchestrator subscribed to topics: {string.Join(", ", topics)}");

                while (!stoppingToken.IsCancellationRequested)
                {
                    try
                    {
                        var consumeResult = consumer.Consume(TimeSpan.FromSeconds(5));
                        if (consumeResult?.Message?.Value == null) 
                        {
                            continue;
                        }

                        _logger.LogInformation($"Received message from topic {consumeResult.Topic}: {consumeResult.Message.Value}");

                        await ProcessMessageAsync(consumeResult.Topic, consumeResult.Message.Value);
                    }
                    catch (ConsumeException ex)
                    {
                        _logger.LogError(ex, $"Kafka consume error: {ex.Error.Reason}");
                        await Task.Delay(5000, stoppingToken);
                    }
                    catch (OperationCanceledException)
                    {
                        break;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Saga orchestrator error");
                        await Task.Delay(1000, stoppingToken);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to initialize Kafka consumer. Service will continue without Kafka integration.");
            }
            finally
            {
                consumer?.Close();
                consumer?.Dispose();
                _logger.LogInformation("Saga Orchestrator Background Service stopped.");
            }
        }

        private async Task ProcessMessageAsync(string topic, string messageValue)
        {
            try
            {
                // Create a scope to get scoped services
                using var scope = _serviceScopeFactory.CreateScope();
                var sagaFlowEngine = scope.ServiceProvider.GetRequiredService<SagaFlowEngine>();

                if (topic == "saga-orchestration")
                {
                    var sagaStartRequest = JsonSerializer.Deserialize<SagaStartRequest>(messageValue);
                    if (sagaStartRequest != null)
                    {
                        await HandleSagaStartRequestAsync(sagaStartRequest, sagaFlowEngine);
                    }
                }
                else
                {
                    var sagaEvent = JsonSerializer.Deserialize<SagaEvent>(messageValue);
                    if (sagaEvent != null)
                    {
                        await sagaFlowEngine.ProcessSagaEventAsync(sagaEvent);
                    }
                }
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, $"Failed to deserialize message from topic {topic}: {messageValue}");
            }
        }

        private async Task HandleSagaStartRequestAsync(SagaStartRequest request, SagaFlowEngine sagaFlowEngine)
        {
            try
            {
                _logger.LogInformation($"Starting saga for flow: {request.FlowName}");
                
                var sagaId = await sagaFlowEngine.StartSagaAsync(request.FlowName, request.InitialData);
                
                _logger.LogInformation($"Started saga {sagaId} for flow {request.FlowName}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to start saga for flow {request.FlowName}");
            }
        }
    }

    public class SagaStartRequest
    {
        public string FlowName { get; set; } = string.Empty;
        public Dictionary<string, object> InitialData { get; set; } = new();
    }
}
