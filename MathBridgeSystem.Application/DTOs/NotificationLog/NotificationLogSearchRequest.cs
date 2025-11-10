using System;

namespace MathBridgeSystem.Application.DTOs.NotificationLog
{
    public class NotificationLogSearchRequest
    {
        public Guid? NotificationId { get; set; }
        public Guid? ContractId { get; set; }
        public Guid? SessionId { get; set; }
        public string? Channel { get; set; }
        public string? Status { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
    }
}