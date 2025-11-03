using System;

namespace MathBridgeSystem.Application.DTOs
{
    public class RescheduleResponseDto
    {
        public Guid RequestId { get; set; }
        public string Status { get; set; } = null!;
        public string Message { get; set; } = null!;
        public DateTime? ProcessedDate { get; set; }
    }
}