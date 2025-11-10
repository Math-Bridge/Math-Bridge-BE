using System;

namespace MathBridgeSystem.Application.DTOs.NotificationLog
{
    public class NotificationLogDto
    {
        public Guid LogId { get; set; }
        public Guid NotificationId { get; set; }
        public Guid? ContractId { get; set; }
        public Guid? SessionId { get; set; }
        public string Channel { get; set; } = null!;
        public string Status { get; set; } = null!;
        public string? ErrorMessage { get; set; }
        public DateTime CreatedDate { get; set; }
        
        // Navigation properties for display
        public string? NotificationTitle { get; set; }
        public string? NotificationMessage { get; set; }
    }
}