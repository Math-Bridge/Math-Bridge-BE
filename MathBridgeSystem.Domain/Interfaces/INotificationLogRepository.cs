using MathBridgeSystem.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MathBridgeSystem.Domain.Interfaces
{
    public interface INotificationLogRepository
    {
        Task<NotificationLog?> GetByIdAsync(Guid logId);
        Task<List<NotificationLog>> GetAllAsync();
        Task<List<NotificationLog>> GetByNotificationIdAsync(Guid notificationId);
        Task<List<NotificationLog>> GetByContractIdAsync(Guid contractId);
        Task<List<NotificationLog>> GetBySessionIdAsync(Guid sessionId);
        Task<List<NotificationLog>> GetByChannelAsync(string channel);
        Task<List<NotificationLog>> GetByStatusAsync(string status);
        Task<List<NotificationLog>> GetFailedLogsAsync();
        Task<List<NotificationLog>> GetLogsByDateRangeAsync(DateTime startDate, DateTime endDate);
        Task AddAsync(NotificationLog notificationLog);
        Task UpdateAsync(NotificationLog notificationLog);
        Task DeleteAsync(Guid logId);
    }
}