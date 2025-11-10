using MathBridgeSystem.Application.DTOs.NotificationTemplate;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MathBridgeSystem.Application.Interfaces
{
    public interface INotificationTemplateService
    {
        Task<NotificationTemplateDto?> GetByIdAsync(Guid templateId);
        Task<List<NotificationTemplateDto>> GetAllAsync();
        Task<NotificationTemplateDto?> GetByNameAsync(string name);
        Task<List<NotificationTemplateDto>> GetByNotificationTypeAsync(string notificationType);
        Task<List<NotificationTemplateDto>> GetActiveTemplatesAsync();
        Task<NotificationTemplateDto> CreateAsync(CreateNotificationTemplateRequest request);
        Task<NotificationTemplateDto?> UpdateAsync(Guid templateId, UpdateNotificationTemplateRequest request);
        Task<bool> DeleteAsync(Guid templateId);
        Task<bool> ToggleActiveStatusAsync(Guid templateId);
    }
}