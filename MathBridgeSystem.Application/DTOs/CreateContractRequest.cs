using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MathBridgeSystem.Application.DTOs
{
    public class CreateContractRequest
    {
        public Guid ParentId { get; set; }
        public Guid ChildId { get; set; }
        public Guid PackageId { get; set; }
        public Guid MainTutorId { get; set; }
        public Guid? SubstituteTutor1Id { get; set; }
        public Guid? SubstituteTutor2Id { get; set; }
        public Guid? CenterId { get; set; }
        public DateOnly StartDate { get; set; }
        public DateOnly EndDate { get; set; }
        public byte? DaysOfWeeks { get; set; }
        public TimeOnly? StartTime { get; set; }
        public TimeOnly? EndTime { get; set; }
        /// <summary>
        /// Bitmask for contract schedule days (nullable). Sunday=1 (bit 0), Monday=2 (bit 1),
        /// Tuesday=4 (bit 2), Wednesday=8 (bit 3), Thursday=16 (bit 4), Friday=32 (bit 5),
        /// Saturday=64 (bit 6). Valid: 1-127 (at least one day selected). All days=127.
        /// Common: Weekdays Mon-Fri=62 (2+4+8+16+32), Weekends Sat-Sun=65 (1+64),
        /// Mon Wed Fri=42 (2+8+32), Tue Thu=20 (4+16).
        /// </summary>
        public bool IsOnline { get; set; }
        public string? OfflineAddress { get; set; }
        public decimal? OfflineLatitude { get; set; }
        public decimal? OfflineLongitude { get; set; }
        public string? VideoCallPlatform { get; set; }
        public decimal MaxDistanceKm { get; set; }
    }
}
