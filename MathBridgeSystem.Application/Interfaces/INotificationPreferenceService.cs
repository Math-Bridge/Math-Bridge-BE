using MathBridgeSystem.Application.DTOs.NotificationPreference;
using System;
using System.Threading.Tasks;

namespace MathBridgeSystem.Application.Interfaces
{
    public interface INotificationPreferenceService
    {
        Task<NotificationPreferenceDto> GetPreferencesByUserIdAsync(Guid userId);
        Task<Guid> CreateOrUpdatePreferencesAsync(Guid userId, UpdateNotificationPreferenceRequest request);
        Task<NotificationPreferenceDto> GetOrCreateDefaultPreferencesAsync(Guid userId);
    }
}