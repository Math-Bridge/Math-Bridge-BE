using System;

namespace MathBridgeSystem.Application.DTOs
{
    public class RescheduleRequestDto
    {
        public Guid RequestId { get; set; }
        public Guid BookingId { get; set; }
        public Guid ParentId { get; set; }
        public string ParentName { get; set; } = null!;
        public Guid ChildId { get; set; }
        public string ChildName { get; set; } = null!;
        public Guid ContractId { get; set; }
        public DateOnly RequestedDate { get; set; }
        public TimeOnly StartTime { get; set; }
        public TimeOnly EndTime { get; set; }
        public Guid? RequestedTutorId { get; set; }
        public string? RequestedTutorName { get; set; }
        public string? Reason { get; set; }
        public string Status { get; set; } = null!;
        public Guid? StaffId { get; set; }
        public string? StaffName { get; set; }
        public DateTime? ProcessedDate { get; set; }
        public DateTime CreatedDate { get; set; }
        
        // Session details
        public DateOnly OriginalSessionDate { get; set; }
        public DateTime OriginalStartTime { get; set; }
        public DateTime OriginalEndTime { get; set; }
        public Guid OriginalTutorId { get; set; }
        public string OriginalTutorName { get; set; } = null!;

    }
}

