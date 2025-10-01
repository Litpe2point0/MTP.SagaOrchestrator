using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Architecture_1.DataAccess.Entities
{
    public class SagaInstance
    {
        public Guid SagaId { get; set; }
        public string FlowName { get; set; }
        public string? CurrentStepName { get; set; }
        public Dictionary<string, object> InitialData { get; set; } = new();
        public Dictionary<string, object> ResultData { get; set; } = new();
        public SagaStatus FlowStatus { get; set; }
        public string? ErrorStepName { get; set; }
        public string? ErrorMessage { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? CompletedAt { get; set; }
    }
    public enum SagaStatus
    {
        RUNNING,
        SUCCESS,
        FAILED
    }
}
