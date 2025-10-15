using System;
using System.Collections.Generic;

namespace MathBridgeSystem.Domain.Entities;

public partial class TutorVerification
{
    public Guid VerificationId { get; set; }

    public Guid UserId { get; set; }

    public string University { get; set; } = null!;

    public string Major { get; set; } = null!;

    public decimal HourlyRate { get; set; }

    public string? Bio { get; set; }

    public string VerificationStatus { get; set; } = null!;

    public DateTime? VerificationDate { get; set; }

    public DateTime CreatedDate { get; set; }

    public virtual User User { get; set; } = null!;
}
