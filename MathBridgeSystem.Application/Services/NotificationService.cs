using MathBridgeSystem.Application.DTOs.Notification;
using MathBridgeSystem.Application.Interfaces;
using MathBridgeSystem.Domain.Entities;
using MathBridgeSystem.Domain.Interfaces;
using MathBridgeSystem.Infrastructure.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MathBridgeSystem.Application.Services
{
    public class NotificationService : INotificationService
    {
        private readonly INotificationRepository _notificationRepository;
        private readonly NotificationConnectionManager _connectionManager;
        private readonly IPubSubNotificationProvider _pubSubProvider;

        public NotificationService(
            INotificationRepository notificationRepository,
            NotificationConnectionManager connectionManager,
            IPubSubNotificationProvider pubSubProvider = null)
        {
            _notificationRepository = notificationRepository ?? throw new ArgumentNullException(nameof(notificationRepository));
            _connectionManager = connectionManager ?? throw new ArgumentNullException(nameof(connectionManager));
            _pubSubProvider = pubSubProvider; // Can be null for SSE-only mode
        }

        public async Task<NotificationResponseDto> CreateNotificationAsync(CreateNotificationRequest request)
        {
            var notification = new Notification
            {
                NotificationId = Guid.NewGuid(),
                UserId = request.UserId,
                ContractId = request.ContractId,
                BookingId = request.BookingId,
                Title = request.Title,
                Message = request.Message,
                NotificationType = request.NotificationType,
                Status = "Pending",
                CreatedDate = DateTime.UtcNow
            };

            await _notificationRepository.AddAsync(notification);
            
            var dto = MapToDto(notification);
            
            // Send immediately via SSE to connected users (maybe duplicate with  HandleMessageAsync)
           // await _connectionManager.SendNotificationAsync(notification.UserId, dto);
            
            // Also publish to Pub/Sub if available (for multi-server scalability)
            if (_pubSubProvider != null)
            {
                await _pubSubProvider.PublishNotificationAsync(dto, "notifications");
            }
            
            // Mark as sent
            notification.Status = "Sent";
            notification.SentDate = DateTime.UtcNow;
            await _notificationRepository.UpdateAsync(notification);

            return dto;
        }

        public async Task<NotificationResponseDto?> GetNotificationByIdAsync(Guid notificationId)
        {
            var notification = await _notificationRepository.GetByIdAsync(notificationId);
            return notification == null ? null : MapToDto(notification);
        }

        public async Task<List<NotificationResponseDto>> GetNotificationsByUserIdAsync(Guid userId, int pageNumber = 1, int pageSize = 10)
        {
            var notifications = await _notificationRepository.GetPaginatedByUserIdAsync(userId, pageNumber, pageSize);
            return notifications.Select(MapToDto).ToList();
        }

        public async Task<List<NotificationResponseDto>> GetUnreadNotificationsByUserIdAsync(Guid userId)
        {
            var notifications = await _notificationRepository.GetUnreadByUserIdAsync(userId);
            return notifications.Select(MapToDto).ToList();
        }

        public async Task<int> GetUnreadCountAsync(Guid userId)
        {
            return await _notificationRepository.GetUnreadCountAsync(userId);
        }

        public async Task MarkAsReadAsync(Guid notificationId)
        {
            await _notificationRepository.MarkAsReadAsync(notificationId);
        }

        public async Task MarkAllAsReadAsync(Guid userId)
        {
            await _notificationRepository.MarkAllAsReadAsync(userId);
        }

        public async Task DeleteNotificationAsync(Guid notificationId)
        {
            await _notificationRepository.DeleteAsync(notificationId);
        }

        public async Task DeleteAllNotificationsAsync(Guid userId)
        {
            await _notificationRepository.DeleteAllAsync(userId);
        }

        public async Task PublishToPubSubAsync(NotificationResponseDto notification, string topic)
        {
            if (_pubSubProvider != null)
            {
                await _pubSubProvider.PublishNotificationAsync(notification, topic);
            }
        }

        private static NotificationResponseDto MapToDto(Notification notification)
        {
            return new NotificationResponseDto
            {
                NotificationId = notification.NotificationId,
                UserId = notification.UserId,
                ContractId = notification.ContractId,
                BookingId = notification.BookingId,
                Title = notification.Title,
                Message = notification.Message,
                NotificationType = notification.NotificationType,
                Status = notification.Status,
                CreatedDate = notification.CreatedDate,
                SentDate = notification.SentDate,
                IsRead = notification.Status == "Read"
            };
        }
    }
}