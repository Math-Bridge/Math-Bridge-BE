using System;
using System.Collections.Generic;

namespace MathBridge.Domain.Entities;

public partial class Review
{
    public Guid ReviewId { get; set; }

    public Guid UserId { get; set; }

    public int Rating { get; set; }

    public string? ReviewTitle { get; set; }

    public string ReviewText { get; set; } = null!;

    public string ReviewStatus { get; set; } = null!;

    public DateTime CreatedDate { get; set; }

    public virtual User User { get; set; } = null!;
}
