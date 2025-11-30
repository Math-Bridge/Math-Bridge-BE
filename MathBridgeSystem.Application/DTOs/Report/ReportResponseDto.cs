using System;
using MathBridgeSystem.Application.DTOs;

namespace MathBridgeSystem.Application.DTOs.Report
{
    public class ReportResponseDto
    {
        public Guid ReportId { get; set; }
        public Guid ParentId { get; set; }
        public UserResponse Parent { get; set; } = null!;
        public Guid TutorId { get; set; }
        public UserResponse Tutor { get; set; } = null!;
        public string Content { get; set; } = null!;
        public string? Url { get; set; }
        public string Status { get; set; } = null!;
        public DateOnly CreatedDate { get; set; }
        public string? Type { get; set; }
        public Guid? ContractId { get; set; }
    }
}