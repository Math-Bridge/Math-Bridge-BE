using System;
using System.ComponentModel.DataAnnotations;

namespace MathBridgeSystem.Application.DTOs.NotificationLog
{
    public class CreateNotificationLogRequest
    {
        [Required]
        public Guid NotificationId { get; set; }
        
        public Guid? ContractId { get; set; }
        
        public Guid? SessionId { get; set; }
        
        [Required]
        [StringLength(50)]
        public string Channel { get; set; } = null!;
        
        [Required]
        [StringLength(50)]
        public string Status { get; set; } = null!;
        
        [StringLength(500)]
        public string? ErrorMessage { get; set; }
    }
}