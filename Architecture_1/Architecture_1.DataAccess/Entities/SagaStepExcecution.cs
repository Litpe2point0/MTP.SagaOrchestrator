using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Architecture_1.DataAccess.Entities
{
    public class SagaStepExcecution
    {
        public int Id { get; set; }
        public Guid SagaId { get; set; }
        public string StepName { get; set; }
        public string? TopicName { get; set; }
        public StepStatus StepStatus { get; set; }
        public Dictionary<string, object>? RequestData { get; set; } = new();
        public Dictionary<string, object>? responseData { get; set; } = new();
        public string? ErrorMessage { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
    public enum StepStatus
    {
        RUNNING,
        SUCCESS,
        FAILED
    }
}
