using System;

namespace MathBridgeSystem.Application.DTOs.Report
{
    public class CreateReportDto
    {
        public Guid TutorId { get; set; }
        public string Content { get; set; } = null!;
        public string? Url { get; set; }
        public string? Type { get; set; }
        public Guid ContractId { get; set; }
    }
}