namespace MathBridgeSystem.Domain.Enums;

public enum NotificationType
{
    SessionReminder24hr = 1,
    SessionReminder1hr = 2,
    SessionStarted = 3,
    SessionCompleted = 4,
    SessionCancelled = 5,
    PaymentConfirmed = 6,
    BookingConfirmed = 7,
    RescheduleRequest = 8,
    RescheduleApproved = 9,
    RescheduleRejected = 10,
    TutorAssigned = 11,
    TutorRejected = 12,
    SystemNotification = 13
}