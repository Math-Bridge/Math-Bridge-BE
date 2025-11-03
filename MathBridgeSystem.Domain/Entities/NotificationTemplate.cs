using System;
using System.Collections.Generic;

namespace MathBridgeSystem.Domain.Entities;

public partial class NotificationTemplate
{
    public Guid TemplateId { get; set; }

    public string TemplateName { get; set; } = null!;

    public string NotificationType { get; set; } = null!;

    public string Subject { get; set; } = null!;

    public string Body { get; set; } = null!;

    public string? HtmlBody { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedDate { get; set; }

    public DateTime? UpdatedDate { get; set; }
}