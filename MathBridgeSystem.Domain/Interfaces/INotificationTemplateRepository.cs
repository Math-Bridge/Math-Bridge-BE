using MathBridgeSystem.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MathBridgeSystem.Domain.Interfaces
{
    public interface INotificationTemplateRepository
    {
        Task<NotificationTemplate?> GetByIdAsync(Guid templateId);
        Task<List<NotificationTemplate>> GetAllAsync();
        Task<NotificationTemplate?> GetByNameAsync(string name);
        Task<List<NotificationTemplate>> GetByNotificationTypeAsync(string notificationType);
        Task<List<NotificationTemplate>> GetActiveTemplatesAsync();
        Task AddAsync(NotificationTemplate template);
        Task UpdateAsync(NotificationTemplate template);
        Task DeleteAsync(Guid templateId);
    }
}