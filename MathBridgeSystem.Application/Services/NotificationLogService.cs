using MathBridgeSystem.Application.DTOs.NotificationLog;
using MathBridgeSystem.Application.Interfaces;
using MathBridgeSystem.Domain.Entities;
using MathBridgeSystem.Domain.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MathBridgeSystem.Application.Services
{
    public class NotificationLogService : INotificationLogService
    {
        private readonly INotificationLogRepository _notificationLogRepository;

        public NotificationLogService(INotificationLogRepository notificationLogRepository)
        {
            _notificationLogRepository = notificationLogRepository ?? throw new ArgumentNullException(nameof(notificationLogRepository));
        }

        public async Task<NotificationLogDto?> GetByIdAsync(Guid logId)
        {
            var log = await _notificationLogRepository.GetByIdAsync(logId);
            return log != null ? MapToDto(log) : null;
        }

        public async Task<List<NotificationLogDto>> GetAllAsync()
        {
            var logs = await _notificationLogRepository.GetAllAsync();
            return logs.Select(MapToDto).ToList();
        }

        public async Task<List<NotificationLogDto>> GetByNotificationIdAsync(Guid notificationId)
        {
            var logs = await _notificationLogRepository.GetByNotificationIdAsync(notificationId);
            return logs.Select(MapToDto).ToList();
        }

        public async Task<List<NotificationLogDto>> GetByContractIdAsync(Guid contractId)
        {
            var logs = await _notificationLogRepository.GetByContractIdAsync(contractId);
            return logs.Select(MapToDto).ToList();
        }

        public async Task<List<NotificationLogDto>> GetBySessionIdAsync(Guid sessionId)
        {
            var logs = await _notificationLogRepository.GetBySessionIdAsync(sessionId);
            return logs.Select(MapToDto).ToList();
        }

        public async Task<List<NotificationLogDto>> GetByChannelAsync(string channel)
        {
            var logs = await _notificationLogRepository.GetByChannelAsync(channel);
            return logs.Select(MapToDto).ToList();
        }

        public async Task<List<NotificationLogDto>> GetByStatusAsync(string status)
        {
            var logs = await _notificationLogRepository.GetByStatusAsync(status);
            return logs.Select(MapToDto).ToList();
        }

        public async Task<List<NotificationLogDto>> GetFailedLogsAsync()
        {
            var logs = await _notificationLogRepository.GetFailedLogsAsync();
            return logs.Select(MapToDto).ToList();
        }

        public async Task<List<NotificationLogDto>> SearchLogsAsync(NotificationLogSearchRequest request)
        {
            var logs = await _notificationLogRepository.GetAllAsync();

            // Apply filters
            if (request.NotificationId.HasValue)
            {
                logs = logs.Where(l => l.NotificationId == request.NotificationId.Value).ToList();
            }

            if (request.ContractId.HasValue)
            {
                logs = logs.Where(l => l.ContractId == request.ContractId.Value).ToList();
            }

            if (request.SessionId.HasValue)
            {
                logs = logs.Where(l => l.SessionId == request.SessionId.Value).ToList();
            }

            if (!string.IsNullOrEmpty(request.Channel))
            {
                logs = logs.Where(l => l.Channel == request.Channel).ToList();
            }

            if (!string.IsNullOrEmpty(request.Status))
            {
                logs = logs.Where(l => l.Status == request.Status).ToList();
            }

            if (request.StartDate.HasValue)
            {
                logs = logs.Where(l => l.CreatedDate >= request.StartDate.Value).ToList();
            }

            if (request.EndDate.HasValue)
            {
                logs = logs.Where(l => l.CreatedDate <= request.EndDate.Value).ToList();
            }

            // Apply pagination
            var pagedLogs = logs
                .Skip((request.PageNumber - 1) * request.PageSize)
                .Take(request.PageSize)
                .ToList();

            return pagedLogs.Select(MapToDto).ToList();
        }

        public async Task<NotificationLogDto> CreateAsync(CreateNotificationLogRequest request)
        {
            var notificationLog = new NotificationLog
            {
                LogId = Guid.NewGuid(),
                NotificationId = request.NotificationId,
                ContractId = request.ContractId,
                SessionId = request.SessionId,
                Channel = request.Channel,
                Status = request.Status,
                ErrorMessage = request.ErrorMessage,
                CreatedDate = DateTime.UtcNow
            };

            await _notificationLogRepository.AddAsync(notificationLog);
            return MapToDto(notificationLog);
        }

        public async Task<bool> DeleteAsync(Guid logId)
        {
            var log = await _notificationLogRepository.GetByIdAsync(logId);
            if (log == null)
            {
                return false;
            }

            await _notificationLogRepository.DeleteAsync(logId);
            return true;
        }

        private NotificationLogDto MapToDto(NotificationLog log)
        {
            return new NotificationLogDto
            {
                LogId = log.LogId,
                NotificationId = log.NotificationId,
                ContractId = log.ContractId,
                SessionId = log.SessionId,
                Channel = log.Channel,
                Status = log.Status,
                ErrorMessage = log.ErrorMessage,
                CreatedDate = log.CreatedDate,
                NotificationTitle = log.Notification?.Title,
                NotificationMessage = log.Notification?.Message
            };
        }
    }
}