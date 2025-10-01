using Confluent.Kafka;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SagaOrchestratorService.Models;
using SagaOrchestratorService.Repositories;
using SagaOrchestratorService.Services;

var builder = WebApplication.CreateBuilder(args);

// Configure logging
builder.Logging.ClearProviders();
builder.Logging.AddConsole();

// Add database context with proper connection string
builder.Services.AddDbContext<SagaDBContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));


// Load YAML flow definitions
var yamlPath = Path.Combine(AppContext.BaseDirectory, "SagaFlows", "order-processing-flow-new.yaml");
if (!File.Exists(yamlPath))
{
    Console.WriteLine($"Warning: YAML flow definition not found at: {yamlPath}");
    // Create a default empty flow definition to prevent startup failure
    var defaultFlow = new SagaFlowDefinition 
    { 
        Version = "1.0", 
        Flows = new Dictionary<string, FlowDefinition>() 
    };
    builder.Services.AddSingleton(defaultFlow);
}
else
{
    try
    {
        var flowDefinition = SagaFlowParser.ParseFromFile(yamlPath);
        builder.Services.AddSingleton(flowDefinition);
        Console.WriteLine($"Successfully loaded YAML flow definition from: {yamlPath}");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error loading YAML: {ex.Message}");
        // Fallback to default
        var defaultFlow = new SagaFlowDefinition 
        { 
            Version = "1.0", 
            Flows = new Dictionary<string, FlowDefinition>() 
        };
        builder.Services.AddSingleton(defaultFlow);
    }
}

// Configure Kafka Producer with more lenient settings
builder.Services.AddSingleton<IProducer<string, string>>(provider =>
{
    var configuration = provider.GetRequiredService<IConfiguration>();
    var config = new ProducerConfig
    {
        BootstrapServers = configuration["Kafka:BootstrapServers"] ?? "172.26.128.1:9092",
        ClientId = "saga-orchestrator",
        EnableIdempotence = false,  // Disable idempotence to avoid coordinator issues
        Acks = Acks.Leader,         // Less strict acknowledgment
        RetryBackoffMs = 5000,      // Longer backoff
        MessageSendMaxRetries = 2,  // Fewer retries
        RequestTimeoutMs = 10000,   // Shorter timeout
        MessageTimeoutMs = 30000,
        LingerMs = 5               // Small batching delay
    };
    
    var producer = new ProducerBuilder<string, string>(config)
        .SetErrorHandler((_, e) => Console.WriteLine($"Kafka Producer Error: {e.Reason}"))
        .SetLogHandler((_, log) => Console.WriteLine($"Kafka Producer Log: {log.Message}"))
        .Build();
    
    return producer;
});

// Register services
builder.Services.AddScoped<SagaFlowEngine>();
builder.Services.AddHostedService<SagaOrchestratorBackgroundService>();
builder.Services.AddScoped<ISagaRepository, SagaRepository>();
builder.Services.AddScoped<ISagaLogger, SagaLogger>();
builder.Services.AddSingleton<ISagaUpdateQueue, SagaUpdateQueue>();

// Add controllers and API support first
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseRouting();
app.MapControllers();

// Listen on all interfaces and use HTTP only for now
app.Urls.Clear();
app.Urls.Add("http://0.0.0.0:5000");

Console.WriteLine("Starting SagaOrchestratorService...");
Console.WriteLine("Swagger UI will be available at: http://localhost:5000/swagger");

await app.RunAsync();
