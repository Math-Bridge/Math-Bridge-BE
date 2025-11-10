using MathBridgeSystem.Application.DTOs.NotificationLog;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MathBridgeSystem.Application.Interfaces
{
    public interface INotificationLogService
    {
        Task<NotificationLogDto?> GetByIdAsync(Guid logId);
        Task<List<NotificationLogDto>> GetAllAsync();
        Task<List<NotificationLogDto>> GetByNotificationIdAsync(Guid notificationId);
        Task<List<NotificationLogDto>> GetByContractIdAsync(Guid contractId);
        Task<List<NotificationLogDto>> GetBySessionIdAsync(Guid sessionId);
        Task<List<NotificationLogDto>> GetByChannelAsync(string channel);
        Task<List<NotificationLogDto>> GetByStatusAsync(string status);
        Task<List<NotificationLogDto>> GetFailedLogsAsync();
        Task<List<NotificationLogDto>> SearchLogsAsync(NotificationLogSearchRequest request);
        Task<NotificationLogDto> CreateAsync(CreateNotificationLogRequest request);
        Task<bool> DeleteAsync(Guid logId);
    }
}