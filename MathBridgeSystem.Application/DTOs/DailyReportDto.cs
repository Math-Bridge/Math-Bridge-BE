using System;
using System.ComponentModel.DataAnnotations;

namespace MathBridgeSystem.Application.DTOs.DailyReport
{
    public class DailyReportDto
    {
        public Guid ReportId { get; set; }
        public Guid ChildId { get; set; }
        public Guid TutorId { get; set; }
        public Guid BookingId { get; set; }
        public string? Notes { get; set; }
        public bool OnTrack { get; set; }
        public bool HaveHomework { get; set; }
        public DateOnly CreatedDate { get; set; }
        public Guid UnitId { get; set; }
        public Guid? TestId { get; set; }
    }

    public class CreateDailyReportRequest
    {
        [Required]
        public Guid ChildId { get; set; }

        [Required]
        public Guid BookingId { get; set; }

        [MaxLength(1000)]
        public string? Notes { get; set; }

        [Required]
        public bool OnTrack { get; set; }

        [Required]
        public bool HaveHomework { get; set; }

        [Required]
        public Guid UnitId { get; set; }
        
    }

    public class UpdateDailyReportRequest
    {
        [MaxLength(1000)]
        public string? Notes { get; set; }

        public bool? OnTrack { get; set; }

        public bool? HaveHomework { get; set; }

        public Guid? UnitId { get; set; }
        
    }
}

