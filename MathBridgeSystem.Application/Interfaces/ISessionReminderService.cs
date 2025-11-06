using MathBridgeSystem.Application.DTOs.Notification;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MathBridgeSystem.Application.Interfaces;

public interface ISessionReminderService
{
    Task<List<SessionReminderDto>> GetSessionsForReminderAsync(int hoursBeforeSession = 24);
    Task<List<SessionReminderDto>> GetUpcomingSessionsAsync(TimeSpan timeWindow);
    Task CreateReminderNotificationsAsync(List<SessionReminderDto> sessions, string reminderType);
    Task PublishRemindersToTopic(List<SessionReminderDto> sessions, string topic);
    Task CheckAndSendRemindersAsync();
}