using System;

namespace MathBridgeSystem.Application.DTOs
{
    public class ApproveRescheduleRequestDto
    {
        public Guid NewTutorId { get; set; } = Guid.Empty;
        public string? Note { get; set; }
    }
}