using System;

namespace MathBridgeSystem.Application.DTOs.NotificationTemplate
{
    public class NotificationTemplateDto
    {
        public Guid TemplateId { get; set; }
        public string Name { get; set; } = null!;
        public string Subject { get; set; } = null!;
        public string Body { get; set; } = null!;
        public string NotificationType { get; set; } = null!;
        public bool IsActive { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime? UpdatedDate { get; set; }
    }
}