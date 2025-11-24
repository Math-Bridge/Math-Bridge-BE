using System;

namespace MathBridgeSystem.Application.DTOs
{
    public class ContractDto
    {
        public Guid ContractId { get; set; }
        public Guid ChildId { get; set; }
        public Guid ParentId { get; set; }
        public string ChildName { get; set; } = null!;
        public Guid PackageId { get; set; }
        public string PackageName { get; set; } = null!;
        public decimal Price { get; set; }
        public Guid? MainTutorId { get; set; }
        public string? MainTutorName { get; set; } = null!;
        public Guid? substitute_tutor1_id { get; set; }
        public string? substitute_tutor1_name { get; set; }
        public Guid? substitute_tutor2_id { get; set; }
        public string? substitute_tutor2_name { get; set; }
        public Guid? CenterId { get; set; }
        public string? CenterName { get; set; }
        public DateOnly StartDate { get; set; }
        public DateOnly EndDate { get; set; }
        public TimeOnly? StartTime { get; set; }
        public TimeOnly? EndTime { get; set; }
        public byte? DaysOfWeeks { get; set; }
        public string DaysOfWeeksDisplay { get; set; } = null!;
        public bool IsOnline { get; set; }

        // ONLY ONLINE
        public string? VideoCallPlatform { get; set; }

        //ONLY OFFLINE
        public string? OfflineAddress { get; set; }
        public decimal? OfflineLatitude { get; set; }
        public decimal? OfflineLongitude { get; set; }
        public decimal? MaxDistanceKm { get; set; } 

        public int RescheduleCount { get; set; }
        public string Status { get; set; } = null!;
    }
}