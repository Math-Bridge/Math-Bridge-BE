using System;

namespace MathBridgeSystem.Application.DTOs
{
    public class ContractDto
    {
        public Guid ContractId { get; set; }
        public Guid ChildId { get; set; }
        public string ChildName { get; set; } = null!;
        public Guid PackageId { get; set; }
        public string PackageName { get; set; } = null!;
        public Guid? MainTutorId { get; set; }
        public string? MainTutorName { get; set; } = null!;
        public Guid? CenterId { get; set; }
        public string? CenterName { get; set; }
        public DateOnly StartDate { get; set; }
        public DateOnly EndDate { get; set; }
        public TimeOnly? StartTime { get; set; }
        public TimeOnly? EndTime { get; set; }
        public byte? DaysOfWeeks { get; set; }

        /// <summary>
        /// Human-readable display of selected days (e.g., "Mon, Wed, Fri")
        /// </summary>
        public string DaysOfWeeksDisplay { get; set; } = null!;

        public bool IsOnline { get; set; }
        public string Status { get; set; } = null!;
    }
}