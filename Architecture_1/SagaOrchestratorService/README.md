# SagaOrchestrator Service

## Overview

The SagaOrchestrator Service implements the **Saga Pattern** to manage distributed transactions across multiple microservices in the ProductStore system. It coordinates complex business workflows that span multiple services, ensuring data consistency and handling failure scenarios gracefully.

## What is the Saga Pattern?

The Saga pattern is a design pattern for managing distributed transactions in microservices architecture. Instead of using traditional ACID transactions across services, a saga breaks down a business transaction into a series of smaller, local transactions that can be executed independently.

## Architecture

```
┌─────────────────┐    ┌──────────────────────┐    ┌─────────────────────┐
│   OrderService  │    │  SagaOrchestrator    │    │  ProductService     │
│                 │    │                      │    │                     │
│  Creates Order  │───▶│  Orchestrates Saga   │───▶│  Reserves Stock     │
│                 │    │                      │    │                     │
└─────────────────┘    └──────────────────────┘    └─────────────────────┘
         │                         │                          │
         │                         ▼                          │
         │              ┌──────────────────────┐               │
         │              │    Kafka Topics      │               │
         │              │                      │               │
         └──────────────│  • order-created     │◀──────────────┘
                        │  • saga-reply        │
                        │  • inventory-updated │
                        └──────────────────────┘
```

## SagaOrchestrator Functions

### 1. **Event Listening & Processing**
- Subscribes to `order-created` topic from OrderService
- Deserializes and validates incoming order events
- Initiates saga workflows based on event data

### 2. **Saga Workflow Orchestration**
The orchestrator manages the following workflow:

```
Order Created → Reserve Stock → Process Payment → Update Inventory → Confirm Order
     ↓              ↓              ↓              ↓              ↓
   SUCCESS        SUCCESS        SUCCESS        SUCCESS        SUCCESS
     ↓              ↓              ↓              ↓              ↓
     ✓              ✓              ✓              ✓              ✓
```

### 3. **Compensation Handling**
If any step fails, the orchestrator executes compensating actions:

```
Order Created → Reserve Stock → Process Payment → [FAILURE]
     ↓              ↓              ↓
   SUCCESS        SUCCESS        FAILED
     ↓              ↓              ↓
     ✓              ✓              ✗
                    ↓
              Release Stock ← Compensation
```

### 4. **State Management**
- Tracks saga state for each order
- Maintains transaction logs
- Handles retries and timeouts

## Key Components

### SagaOrchestratorBackgroundService
Main service that runs as a background worker:

```csharp
public class SagaOrchestratorBackgroundService : BackgroundService
{
    // Consumes events from Kafka
    // Orchestrates saga steps
    // Publishes saga replies
    // Handles error scenarios
}
```

### Event Models

#### OrderCreatedEvent
```csharp
public class OrderCreatedEvent
{
    public int OrderId { get; set; }
    public int ProductId { get; set; }
    public int Quantity { get; set; }
    public DateTime CreatedAt { get; set; }
}
```

#### SagaReplyEvent
```csharp
public class SagaReplyEvent
{
    public int OrderId { get; set; }
    public string Status { get; set; } // "StockReserved", "PaymentProcessed", "Failed", etc.
    public DateTime Timestamp { get; set; }
}
```

## Workflow Example

### Happy Path Scenario:
1. **OrderService** creates an order and publishes `OrderCreatedEvent`
2. **SagaOrchestrator** receives the event and starts orchestration:
   - Calls ProductService to reserve stock
   - Calls PaymentService to process payment
   - Updates inventory
   - Confirms order completion
3. **SagaOrchestrator** publishes `SagaReplyEvent` with status "OrderCompleted"

### Failure Scenario:
1. **OrderService** creates an order and publishes `OrderCreatedEvent`
2. **SagaOrchestrator** receives the event and starts orchestration:
   - ✅ Calls ProductService to reserve stock (SUCCESS)
   - ❌ Calls PaymentService to process payment (FAILURE)
3. **SagaOrchestrator** executes compensation:
   - Calls ProductService to release reserved stock
   - Marks order as failed
4. **SagaOrchestrator** publishes `SagaReplyEvent` with status "OrderFailed"

## Benefits

### 1. **Data Consistency**
- Ensures eventual consistency across microservices
- Handles partial failures gracefully
- Maintains business invariants

### 2. **Fault Tolerance**
- Automatic retry mechanisms
- Compensation actions for rollback
- Circuit breaker patterns for service failures

### 3. **Observability**
- Comprehensive logging of saga steps
- Event tracking and audit trails
- Monitoring and alerting capabilities

### 4. **Scalability**
- Asynchronous processing
- Event-driven architecture
- Independent service scaling

## Configuration

### Kafka Topics Required:
- `order-created` - Input events from OrderService
- `saga-reply` - Output events to interested services
- `inventory-updated` - Events to/from ProductService

### Environment Setup:
```bash
# Start Kafka
cd "kafka test"
docker-compose up -d

# Create required topics
docker exec kafka kafka-topics --bootstrap-server localhost:9092 --create --topic order-created --partitions 3 --replication-factor 1
docker exec kafka kafka-topics --bootstrap-server localhost:9092 --create --topic saga-reply --partitions 3 --replication-factor 1
```

## Running the Service

```bash
cd SagaOrchestratorService
dotnet build
dotnet run
```

## Monitoring

The service provides extensive logging:
- Saga initiation and completion
- Step-by-step execution logs
- Error handling and compensation logs
- Performance metrics

Example logs:
```
info: SagaOrchestratorService[0]
      Received order event: {"OrderId":123,"ProductId":456,"Quantity":2}
      
info: SagaOrchestratorService[0]
      Orchestrating saga for OrderId: 123, ProductId: 456
      
info: SagaOrchestratorService[0]
      Saga reply sent for OrderId: 123
```

## Future Enhancements

1. **Persistent Saga State** - Store saga state in database for reliability
2. **Timeout Handling** - Implement step timeouts and automatic compensation
3. **Advanced Retry Logic** - Exponential backoff and circuit breakers
4. **Saga Visualization** - Web UI for monitoring saga execution
5. **Integration with More Services** - UserService, PaymentService, NotificationService

## Integration Points

The SagaOrchestrator integrates with:
- **OrderService**: Receives order creation events
- **ProductService**: Coordinates inventory management
- **UserService**: Validates user information
- **PaymentService**: Processes payments
- **NotificationService**: Sends order confirmations

This creates a robust, fault-tolerant system that can handle complex business workflows across multiple microservices while maintaining data consistency and providing excellent observability.
