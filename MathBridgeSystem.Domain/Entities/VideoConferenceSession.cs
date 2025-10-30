using System;
using System.Collections.Generic;

namespace MathBridgeSystem.Domain.Entities;

public partial class VideoConferenceSession
{
    public Guid ConferenceId { get; set; }

    public Guid BookingId { get; set; }

    public Guid ContractId { get; set; }

    public string Platform { get; set; } = null!;

    public string? SpaceName { get; set; }

    public string? SpaceId { get; set; }

    public string? MeetingUri { get; set; }

    public string? MeetingCode { get; set; }

    public string? DisplayName { get; set; }

    public DateTime ScheduledStartTime { get; set; }

    public DateTime ScheduledEndTime { get; set; }

    public DateTime? ActualStartTime { get; set; }

    public DateTime? ActualEndTime { get; set; }

    public string? Status { get; set; }

    public Guid CreatedByUserId { get; set; }

    public DateTime CreatedDate { get; set; }

    public DateTime? UpdatedDate { get; set; }

    public virtual Session Booking { get; set; } = null!;

    public virtual Contract Contract { get; set; } = null!;

    public virtual User CreatedByUser { get; set; } = null!;
}
