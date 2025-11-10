using System.ComponentModel.DataAnnotations;

namespace MathBridgeSystem.Application.DTOs.NotificationTemplate
{
    public class UpdateNotificationTemplateRequest
    {
        [StringLength(100)]
        public string? Name { get; set; }
        
        [StringLength(200)]
        public string? Subject { get; set; }
        
        public string? Body { get; set; }
        
        [StringLength(50)]
        public string? NotificationType { get; set; }
        
        public bool? IsActive { get; set; }
    }
}