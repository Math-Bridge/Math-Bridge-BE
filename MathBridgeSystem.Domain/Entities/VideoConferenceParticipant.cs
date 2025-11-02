using System;
using System.Collections.Generic;

namespace MathBridgeSystem.Domain.Entities;

public partial class VideoConferenceParticipant
{
    public Guid ParticipantId { get; set; }

    public Guid ConferenceId { get; set; }

    public Guid UserId { get; set; }

    public string ParticipantType { get; set; } = null!;

    public DateTime? JoinedAt { get; set; }

    public DateTime? LeftAt { get; set; }

    public int? DurationMinutes { get; set; }

    public string? Status { get; set; }

    public DateTime CreatedDate { get; set; }

    public DateTime? UpdatedDate { get; set; }

    public virtual VideoConferenceSession Conference { get; set; } = null!;

    public virtual User User { get; set; } = null!;
}
