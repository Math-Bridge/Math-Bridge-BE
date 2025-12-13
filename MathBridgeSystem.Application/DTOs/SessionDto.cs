using System;

namespace MathBridgeSystem.Application.DTOs
{
    public class SessionDto
    {
        public Guid BookingId { get; set; }
        public Guid ContractId { get; set; }
        public DateOnly SessionDate { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public string TutorName { get; set; } = null!;
        public bool IsOnline { get; set; }
        public string? VideoCallPlatform { get; set; }
        public string? OfflineAddress { get; set; }
        public string Status { get; set; } = null!;
        public string StudentNames { get; set; } = null!;
        public string PackageName { get; set; } = null!;
    }
}