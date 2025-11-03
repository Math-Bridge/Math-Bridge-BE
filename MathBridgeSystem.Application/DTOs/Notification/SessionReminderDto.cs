using System;

namespace MathBridgeSystem.Application.DTOs.Notification;

public class SessionReminderDto
{
    public Guid SessionId { get; set; }

    public Guid ContractId { get; set; }

    public Guid TutorId { get; set; }

    public Guid ParentId { get; set; }

    public string ReminderType { get; set; } = null!; // "24hr" or "1hr"

    public DateTime SessionStartTime { get; set; }

    public string? StudentName { get; set; }

    public string? TutorName { get; set; }

    public string? VideoCallPlatform { get; set; }
}