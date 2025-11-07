using System;
using System.Collections.Generic;

namespace MathBridgeSystem.Domain.Entities;

public partial class NotificationTemplate
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
