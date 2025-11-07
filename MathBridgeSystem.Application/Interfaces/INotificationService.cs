using MathBridgeSystem.Application.DTOs.Notification;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MathBridgeSystem.Application.Interfaces;

public interface INotificationService
{
    Task<NotificationResponseDto> CreateNotificationAsync(CreateNotificationRequest request);
    Task<NotificationResponseDto?> GetNotificationByIdAsync(Guid notificationId);
    Task<List<NotificationResponseDto>> GetNotificationsByUserIdAsync(Guid userId, int pageNumber = 1, int pageSize = 10);
    Task<List<NotificationResponseDto>> GetUnreadNotificationsByUserIdAsync(Guid userId);
    Task<int> GetUnreadCountAsync(Guid userId);
    Task MarkAsReadAsync(Guid notificationId);
    Task MarkAllAsReadAsync(Guid userId);
    Task DeleteNotificationAsync(Guid notificationId);
    Task DeleteAllNotificationsAsync(Guid userId);
    Task PublishToPubSubAsync(NotificationResponseDto notification, string topic);
}