using MathBridgeSystem.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MathBridgeSystem.Domain.Interfaces;

public interface INotificationLogRepository
{
    Task AddAsync(NotificationLog log);
    Task UpdateAsync(NotificationLog log);
    Task<NotificationLog?> GetByIdAsync(Guid logId);
    Task<List<NotificationLog>> GetByNotificationIdAsync(Guid notificationId);
    Task<List<NotificationLog>> GetByContractIdAsync(Guid contractId);
    Task<List<NotificationLog>> GetBySessionIdAsync(Guid sessionId);
    Task<List<NotificationLog>> GetFailedLogsAsync(int pageNumber, int pageSize);
    Task<int> GetFailedCountAsync();
}