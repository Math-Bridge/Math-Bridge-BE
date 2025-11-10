using System;
using System.Collections.Generic;

namespace MathBridgeSystem.Domain.Entities;

public partial class SupportRequest
{
    public Guid RequestId { get; set; }

    public Guid UserId { get; set; }

    public Guid? AssignedToUserId { get; set; }

    public string Subject { get; set; } = null!;

    public string Description { get; set; } = null!;

    public string Category { get; set; } = null!;

    public string Status { get; set; } = null!;

    public string? Resolution { get; set; }

    public string? AdminNotes { get; set; }

    public DateTime CreatedDate { get; set; }

    public DateTime UpdatedDate { get; set; }

    public DateTime? ResolvedDate { get; set; }

    public virtual User? AssignedToUser { get; set; }

    public virtual User User { get; set; } = null!;
}
