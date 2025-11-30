using System;

namespace MathBridgeSystem.Application.DTOs.Report
{
    public class UpdateReportStatusDto
    {
        public string Status { get; set; } = null!;
        public string? Reason { get; set; }
    }
}