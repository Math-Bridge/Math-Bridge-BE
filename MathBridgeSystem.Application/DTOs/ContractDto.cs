using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MathBridgeSystem.Application.DTOs
{
    public class ContractDto
    {
        public Guid ContractId { get; set; }
        public Guid ChildId { get; set; }
        public string ChildName { get; set; }
        public Guid PackageId { get; set; }
        public string PackageName { get; set; }
        public Guid? MainTutorId { get; set; }
        public string MainTutorName { get; set; }
        public Guid? CenterId { get; set; }
        public string? CenterName { get; set; }
        public DateOnly StartDate { get; set; }
        public DateOnly EndDate { get; set; }
        public TimeOnly? StartTime { get; set; }
        public TimeOnly? EndTime { get; set; }
        public byte? DaysOfWeeks { get; set; }
/// <summary>
        /// Bitmask value for contract schedule days. Sunday=1 (bit 0), Monday=2 (bit 1),
        /// Tuesday=4 (bit 2), Wednesday=8 (bit 3), Thursday=16 (bit 4), Friday=32 (bit 5),
        /// Saturday=64 (bit 6). All days=127. Examples: Weekdays=62, Mon Wed Fri=42.
        /// </summary>
        public string DaysOfWeeksDisplay { get; set; }
/// <summary>
        /// Human-readable display of selected days from DaysOfWeeks bitmask
        /// (e.g., "Mon, Wed, Fri" for value 42). Formatted in service layer.
        /// </summary>
        public bool IsOnline { get; set; }
        public string Status { get; set; }
    }
}
