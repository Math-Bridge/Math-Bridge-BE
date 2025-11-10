using System;

namespace MathBridgeSystem.Application.DTOs.NotificationPreference
{
    public class NotificationPreferenceDto
    {
        public Guid PreferenceId { get; set; }
        public Guid UserId { get; set; }
        public bool ReceiveEmailNotifications { get; set; }
        public bool ReceiveSmsNotifications { get; set; }
        public bool ReceiveWebNotifications { get; set; }
        public bool ReceiveSessionReminders { get; set; }
        public bool ReceiveContractUpdates { get; set; }
        public bool ReceivePaymentNotifications { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime? UpdatedDate { get; set; }
    }

    public class UpdateNotificationPreferenceRequest
    {
        public bool? ReceiveEmailNotifications { get; set; }
        public bool? ReceiveSmsNotifications { get; set; }
        public bool? ReceiveWebNotifications { get; set; }
        public bool? ReceiveSessionReminders { get; set; }
        public bool? ReceiveContractUpdates { get; set; }
        public bool? ReceivePaymentNotifications { get; set; }
    }
}