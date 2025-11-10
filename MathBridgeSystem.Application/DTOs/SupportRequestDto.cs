using System;

namespace MathBridgeSystem.Application.DTOs
{
    public class SupportRequestDto
    {
        public Guid RequestId { get; set; }
        public Guid UserId { get; set; }
        public string UserName { get; set; } = null!;
        public Guid? AssignedToUserId { get; set; }
        public string? AssignedToUserName { get; set; }
        public string Subject { get; set; } = null!;
        public string Description { get; set; } = null!;
        public string Category { get; set; } = null!;
        public string Status { get; set; } = null!;
        public string? Resolution { get; set; }
        public string? AdminNotes { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime UpdatedDate { get; set; }
        public DateTime? ResolvedDate { get; set; }
    }

    public class CreateSupportRequestRequest
    {
        public string Subject { get; set; } = null!;
        public string Description { get; set; } = null!;
        public string Category { get; set; } = null!;
    }

    public class UpdateSupportRequestRequest
    {
        public string Subject { get; set; } = null!;
        public string Description { get; set; } = null!;
        public string Category { get; set; } = null!;
    }

    public class AssignSupportRequestRequest
    {
        public Guid AssignedToUserId { get; set; }
    }

    public class UpdateSupportRequestStatusRequest
    {
        public string Status { get; set; } = null!;
        public string? Resolution { get; set; }
        public string? AdminNotes { get; set; }
    }
}