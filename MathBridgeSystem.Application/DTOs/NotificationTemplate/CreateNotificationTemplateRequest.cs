using System.ComponentModel.DataAnnotations;

namespace MathBridgeSystem.Application.DTOs.NotificationTemplate
{
    public class CreateNotificationTemplateRequest
    {
        [Required]
        [StringLength(100)]
        public string Name { get; set; } = null!;
        
        [Required]
        [StringLength(200)]
        public string Subject { get; set; } = null!;
        
        [Required]
        public string Body { get; set; } = null!;
        
        [Required]
        [StringLength(50)]
        public string NotificationType { get; set; } = null!;
        
        public bool IsActive { get; set; } = true;
    }
}