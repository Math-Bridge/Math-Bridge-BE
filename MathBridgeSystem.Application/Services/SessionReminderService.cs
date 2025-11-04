using MathBridgeSystem.Application.DTOs.Notification;
using MathBridgeSystem.Application.Interfaces;
using MathBridgeSystem.Domain.Entities;
using MathBridgeSystem.Domain.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MathBridgeSystem.Application.Services
{
    public class SessionReminderService : ISessionReminderService
    {
        private readonly INotificationService _notificationService;
        private readonly IPubSubNotificationProvider _pubSubProvider;
        private readonly ISessionRepository _sessionRepository;
        private const string REMINDERS_TOPIC = "notifications";

        public SessionReminderService(INotificationService notificationService, IPubSubNotificationProvider pubSubProvider, ISessionRepository sessionRepository)
        {
            _notificationService = notificationService ?? throw new ArgumentNullException(nameof(notificationService));
            _pubSubProvider = pubSubProvider ?? throw new ArgumentNullException(nameof(pubSubProvider));
            _sessionRepository = sessionRepository ?? throw new ArgumentNullException(nameof(sessionRepository));
        }

        public async Task<List<SessionReminderDto>> GetSessionsForReminderAsync(int hoursBeforeSession)
        {
            var now = DateTime.Now;
            var startTime = now.AddHours(hoursBeforeSession).AddMinutes(-5);
            var endTime = now.AddHours(hoursBeforeSession + 1).AddMinutes(5);

            var sessions = await _sessionRepository.GetSessionsInTimeRangeAsync(startTime, endTime);
            
            var reminderDtos = sessions.Select(s => new SessionReminderDto
            {
                SessionId = s.BookingId,
                ContractId = s.ContractId,
                TutorId = s.TutorId,
                ParentId = s.Contract.Parent.UserId,
                ReminderType = hoursBeforeSession == 24 ? "24hr" : "1hr",
                SessionStartTime = s.StartTime,
                StudentName = s.Contract.Child.FullName,
                TutorName = s.Tutor.FullName,
                VideoCallPlatform = s.VideoCallPlatform
            }).ToList();

            return reminderDtos;
        }

        public async Task<List<SessionReminderDto>> GetUpcomingSessionsAsync(TimeSpan timeWindow)
        {
            var now = DateTime.UtcNow;
            var endTime = now.Add(timeWindow);

            var sessions = await _sessionRepository.GetSessionsInTimeRangeAsync(now, endTime);
            
            var reminderDtos = sessions.Select(s => new SessionReminderDto
            {
                SessionId = s.BookingId,
                ContractId = s.ContractId,
                TutorId = s.TutorId,
                ParentId = s.Contract.Parent.UserId,
                ReminderType = "upcoming",
                SessionStartTime = s.StartTime,
                StudentName = s.Contract.Child.FullName,
                TutorName = s.Tutor.FullName,
                VideoCallPlatform = s.VideoCallPlatform
            }).ToList();

            return reminderDtos;
        }

        public async Task CreateReminderNotificationsAsync(List<SessionReminderDto> sessions, string reminderType)
        {
            foreach (var session in sessions)
            {
                var title = reminderType == "24hr" ? "Session Tomorrow" : "Session Starting Soon";
                var message = $"Reminder: Your session with {session.TutorName} starts at {session.SessionStartTime:HH:mm}";

                var request = new CreateNotificationRequest
                {
                    UserId = session.ParentId,
                    ContractId = session.ContractId,
                    BookingId = session.SessionId,
                    Title = title,
                    Message = message,
                    NotificationType = reminderType == "24hr" ? "SessionReminder24hr" : "SessionReminder1hr"
                };

                await _notificationService.CreateNotificationAsync(request);
            }
        }

        public async Task PublishRemindersToTopic(List<SessionReminderDto> sessions, string topic)
        {
            var notifications = new List<NotificationResponseDto>();

            foreach (var session in sessions)
            {
                var dto = new NotificationResponseDto
                {
                    NotificationId = Guid.NewGuid(),
                    UserId = session.ParentId,
                    ContractId = session.ContractId,
                    BookingId = session.SessionId,
                    Title = $"Session Reminder",
                    Message = $"Your session starts at {session.SessionStartTime:HH:mm}",
                    NotificationType = "SessionReminder",
                    Status = "Pending",
                    CreatedDate = DateTime.UtcNow
                };
                notifications.Add(dto);
            }

            await _pubSubProvider.PublishBatchNotificationsAsync(notifications, topic);
        }

        public async Task CheckAndSendRemindersAsync()
        {
            // Triggered by Cloud Scheduler or background job
            var sessions24hr = await GetSessionsForReminderAsync(24);
            if (sessions24hr.Any())
            {
                await CreateReminderNotificationsAsync(sessions24hr, "24hr");
                await PublishRemindersToTopic(sessions24hr, REMINDERS_TOPIC);
            }

            var sessions1hr = await GetSessionsForReminderAsync(1);
            if (sessions1hr.Any())
            {
                await CreateReminderNotificationsAsync(sessions1hr, "1hr");
                await PublishRemindersToTopic(sessions1hr, REMINDERS_TOPIC);
            }
        }
    }
}