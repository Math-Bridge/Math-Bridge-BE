using MathBridgeSystem.Domain.Entities;
using System;
using System.Threading.Tasks;

namespace MathBridgeSystem.Domain.Interfaces;

public interface INotificationPreferenceRepository
{
    Task AddAsync(NotificationPreference preference);
    Task UpdateAsync(NotificationPreference preference);
    Task<NotificationPreference?> GetByUserIdAsync(Guid userId);
    Task<bool> ExistsByUserIdAsync(Guid userId);
}