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
        private readonly IContractRepository _contractRepository;
        private readonly ISessionRepository _sessionRepository;
        private readonly IRescheduleRequestRepository _rescheduleRequestRepository;

        public NotificationService(
            INotificationRepository notificationRepository,
            NotificationConnectionManager connectionManager,
            IContractRepository contractRepository,
            ISessionRepository sessionRepository,
            IRescheduleRequestRepository rescheduleRequestRepository,
            IPubSubNotificationProvider pubSubProvider = null)
        {
            _notificationRepository = notificationRepository ?? throw new ArgumentNullException(nameof(notificationRepository));
            _connectionManager = connectionManager ?? throw new ArgumentNullException(nameof(connectionManager));
            _contractRepository = contractRepository ?? throw new ArgumentNullException(nameof(contractRepository));
            _sessionRepository = sessionRepository ?? throw new ArgumentNullException(nameof(sessionRepository));
            _rescheduleRequestRepository = rescheduleRequestRepository ?? throw new ArgumentNullException(nameof(rescheduleRequestRepository));
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
                CreatedDate = DateTime.UtcNow.ToLocalTime()
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
            notification.SentDate = DateTime.UtcNow.ToLocalTime();
            await _notificationRepository.UpdateAsync(notification);

            return dto;
        }

        public async Task<NotificationResponseDto> CreateRescheduleOrRefundNotificationAsync(CreateRescheduleOrRefundNotificationRequest request)
        {
            // Validate Reschedule Request
            var rescheduleRequest = await _rescheduleRequestRepository.GetByIdWithDetailsAsync(request.RequestId);
            if (rescheduleRequest == null)
            {
                throw new KeyNotFoundException($"Reschedule request with ID {request.RequestId} not found.");
            }

            // Update status to approved
            rescheduleRequest.Status = "approved";
            rescheduleRequest.ProcessedDate = DateTime.UtcNow.ToLocalTime();
            await _rescheduleRequestRepository.UpdateAsync(rescheduleRequest);

            // Validate Contract
            var contract = await _contractRepository.GetByIdAsync(request.ContractId);
            if (contract == null)
            {
                throw new KeyNotFoundException($"Contract with ID {request.ContractId} not found.");
            }

            // Validate Session (Booking)
            var session = await _sessionRepository.GetByIdAsync(request.BookingId);
            if (session == null)
            {
                throw new KeyNotFoundException($"Session (Booking) with ID {request.BookingId} not found.");
            }

            // Verify session belongs to contract
            if (session.ContractId != request.ContractId)
            {
                throw new ArgumentException("The specified session does not belong to the provided contract.");
            }

            var notification = new Notification
            {
                NotificationId = Guid.NewGuid(),
                UserId = contract.ParentId, // Send to Parent
                ContractId = request.ContractId,
                BookingId = request.BookingId,
                Title = "Session Action Required",
                Message = $"Please choose to either reschedule or request a refund for the session on {session.SessionDate:dd/MM/yyyy} at {session.StartTime:HH:mm}.",
                NotificationType = "RescheduleOrRefund",
                Status = "Pending",
                CreatedDate = DateTime.UtcNow.ToLocalTime()
            };

            await _notificationRepository.AddAsync(notification);

            var dto = MapToDto(notification);

            // Send immediately via SSE to connected users
            // await _connectionManager.SendNotificationAsync(notification.UserId, dto);

            // Also publish to Pub/Sub if available
            if (_pubSubProvider != null)
            {
                await _pubSubProvider.PublishNotificationAsync(dto, "notifications");
            }

            // Mark as sent
            notification.Status = "Sent";
            notification.SentDate = DateTime.UtcNow.ToLocalTime();
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