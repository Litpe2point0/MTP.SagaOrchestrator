using Microsoft.AspNetCore.Mvc;
using SagaOrchestratorService.Services;
using SagaOrchestratorService.Models;
using System.Text.Json;
using Confluent.Kafka;
using System.Threading.Tasks;

namespace SagaOrchestratorService.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SagaController : ControllerBase
    {
        private readonly ILogger<SagaController> _logger;
        private readonly SagaFlowEngine _sagaFlowEngine;
        private readonly IProducer<string, string> _producer;

        public SagaController(
            ILogger<SagaController> logger,
            SagaFlowEngine sagaFlowEngine,
            IProducer<string, string> producer)
        {
            _logger = logger;
            _sagaFlowEngine = sagaFlowEngine;
            _producer = producer;
        }

        [HttpPost("start")]
        public async Task<IActionResult> StartSaga([FromBody] StartSagaRequest request)
        {
            try
            {
                _logger.LogInformation($"Starting saga for flow: {request.FlowName}");

                var sagaStartRequest = new SagaStartRequest
                {
                    FlowName = request.FlowName,
                    InitialData = request.InitialData
                };

                var message = JsonSerializer.Serialize(sagaStartRequest);
                
                await _producer.ProduceAsync("saga-orchestration", new Message<string, string>
                {
                    Key = Guid.NewGuid().ToString(),
                    Value = message
                });

                return Ok(new { Message = $"Saga start request sent for flow: {request.FlowName}" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to start saga for flow {request.FlowName}");
                return BadRequest(new { Error = ex.Message });
            }
        }

        [HttpPost("start-direct")]
        public async Task<IActionResult> StartSagaDirect([FromBody] StartSagaRequest request)
        {
            try
            {
                _logger.LogInformation($"Starting saga directly for flow: {request.FlowName}");

                var sagaId = await _sagaFlowEngine.StartSagaAsync(request.FlowName, request.InitialData);

                return Ok(new { 
                    SagaId = sagaId,
                    FlowName = request.FlowName,
                    Message = $"Saga {sagaId} started successfully for flow: {request.FlowName}" 
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to start saga for flow {request.FlowName}");
                return BadRequest(new { Error = ex.Message });
            }
        }

        [HttpGet("status/{sagaId}")]
        public async Task<IActionResult> GetSagaStatus(Guid sagaId)
        {
            try
            {
                var saga = await _sagaFlowEngine.GetSagaInstance(sagaId);
                if (saga == null)
                {
                    return NotFound(new { Error = "Saga not found" });
                }

                return Ok(new
                {
                    SagaId = saga.SagaId,
                    FlowName = saga.FlowName,
                    CurrentStep = saga.CurrentStep,
                    Status = saga.Status,
                    StartTime = saga.StartTime,
                    EndTime = saga.EndTime,
                    Steps = saga.Steps,
                    Context = saga.Context
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to get saga status for {sagaId}");
                return BadRequest(new { Error = ex.Message });
            }
        }

        [HttpGet("active")]
        public async Task<IActionResult> GetActiveSagas()
        {
            try
            {
                var activeSagas = await _sagaFlowEngine.GetAllActiveSagas();

                return Ok(activeSagas);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get active sagas");
                return BadRequest(new { Error = ex.Message });
            }
        }

        [HttpPost("test/user-registration")]
        public async Task<IActionResult> TestUserRegistration([FromBody] TestUserRegistrationRequest request)
        {
            try
            {
                var initialData = new Dictionary<string, object>
                {
                    { "userId", request.UserId },
                    { "email", request.Email },
                    { "username", request.Username }
                };

                var sagaStartRequest = new SagaStartRequest
                {
                    FlowName = "user-registration-flow",
                    InitialData = initialData
                };

                var message = JsonSerializer.Serialize(sagaStartRequest);
                
                await _producer.ProduceAsync("saga-orchestration", new Message<string, string>
                {
                    Key = Guid.NewGuid().ToString(),
                    Value = message
                });

                return Ok(new { Message = "User registration saga started", Data = initialData });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to start user registration saga");
                return BadRequest(new { Error = ex.Message });
            }
        }

        [HttpPost("test/order-payment")]
        public async Task<IActionResult> TestOrderPayment([FromBody] TestOrderPaymentRequest request)
        {
            try
            {
                var initialData = new Dictionary<string, object>
                {
                    { "userId", request.UserId },
                    { "productId", request.ProductId },
                    { "quantity", request.Quantity },
                    { "amount", request.Amount }
                };

                var sagaStartRequest = new SagaStartRequest
                {
                    FlowName = "order-payment-flow",
                    InitialData = initialData
                };

                var message = JsonSerializer.Serialize(sagaStartRequest);
                
                await _producer.ProduceAsync("saga-orchestration", new Message<string, string>
                {
                    Key = Guid.NewGuid().ToString(),
                    Value = message
                });

                return Ok(new { Message = "Order payment saga started", Data = initialData });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to start order payment saga");
                return BadRequest(new { Error = ex.Message });
            }
        }
    }

    public class StartSagaRequest
    {
        public string FlowName { get; set; } = string.Empty;
        public Dictionary<string, object> InitialData { get; set; } = new();
    }

    public class TestUserRegistrationRequest
    {
        public string UserId { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
    }

    public class TestOrderPaymentRequest
    {
        public string UserId { get; set; } = string.Empty;
        public string ProductId { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public decimal Amount { get; set; }
    }
}
