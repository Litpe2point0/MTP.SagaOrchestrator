using System;
using System.Collections.Generic;

namespace Architecture_1.Infrastructure.Models.Kafka
{
    // For step commands (messageType == MessageName)
    public class SagaCommandMessage : BaseMessage
    {
        public Guid SagaId { get; set; }
        public string FlowName { get; set; } = string.Empty;
        public string MessageName { get; set; } = string.Empty; // equals MessageType
        public Dictionary<string, object> Data { get; set; } = new();
    }

    // For step outcomes (emits). messageType should be like "create-order.success"
    public class SagaEventMessage : BaseMessage
    {
        public Guid SagaId { get; set; }
        public string FlowName { get; set; } = string.Empty;
        public string MessageName { get; set; } = string.Empty; // equals MessageType (emit)
        public Dictionary<string, object> Data { get; set; } = new();
    }

    // To start a flow by sending messageType == flow name (handled by FlowMessageHandler).
    // No SagaId here; the receiving handler will generate a new one.
    public class StartSagaTriggerMessage : BaseMessage
    {
        public string MessageName { get; set; } = string.Empty;
        public Dictionary<string, object> Data { get; set; } = new();
    }
}