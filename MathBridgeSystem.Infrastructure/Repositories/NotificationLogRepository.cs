using MathBridgeSystem.Domain.Entities;
using MathBridgeSystem.Domain.Interfaces;
using MathBridgeSystem.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MathBridgeSystem.Infrastructure.Repositories
{
    public class NotificationLogRepository : INotificationLogRepository
    {
        private readonly MathBridgeDbContext _context;

        public NotificationLogRepository(MathBridgeDbContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        public async Task<NotificationLog?> GetByIdAsync(Guid logId)
        {
            return await _context.NotificationLogs
                .Include(nl => nl.Notification)
                .Include(nl => nl.Contract)
                .Include(nl => nl.Session)
                .FirstOrDefaultAsync(nl => nl.LogId == logId);
        }

        public async Task<List<NotificationLog>> GetAllAsync()
        {
            return await _context.NotificationLogs
                .Include(nl => nl.Notification)
                .Include(nl => nl.Contract)
                .Include(nl => nl.Session)
                .OrderByDescending(nl => nl.CreatedDate)
                .ToListAsync();
        }

        public async Task<List<NotificationLog>> GetByNotificationIdAsync(Guid notificationId)
        {
            return await _context.NotificationLogs
                .Include(nl => nl.Notification)
                .Include(nl => nl.Contract)
                .Include(nl => nl.Session)
                .Where(nl => nl.NotificationId == notificationId)
                .OrderByDescending(nl => nl.CreatedDate)
                .ToListAsync();
        }

        public async Task<List<NotificationLog>> GetByContractIdAsync(Guid contractId)
        {
            return await _context.NotificationLogs
                .Include(nl => nl.Notification)
                .Include(nl => nl.Contract)
                .Include(nl => nl.Session)
                .Where(nl => nl.ContractId == contractId)
                .OrderByDescending(nl => nl.CreatedDate)
                .ToListAsync();
        }

        public async Task<List<NotificationLog>> GetBySessionIdAsync(Guid sessionId)
        {
            return await _context.NotificationLogs
                .Include(nl => nl.Notification)
                .Include(nl => nl.Contract)
                .Include(nl => nl.Session)
                .Where(nl => nl.SessionId == sessionId)
                .OrderByDescending(nl => nl.CreatedDate)
                .ToListAsync();
        }

        public async Task<List<NotificationLog>> GetByChannelAsync(string channel)
        {
            return await _context.NotificationLogs
                .Include(nl => nl.Notification)
                .Include(nl => nl.Contract)
                .Include(nl => nl.Session)
                .Where(nl => nl.Channel == channel)
                .OrderByDescending(nl => nl.CreatedDate)
                .ToListAsync();
        }

        public async Task<List<NotificationLog>> GetByStatusAsync(string status)
        {
            return await _context.NotificationLogs
                .Include(nl => nl.Notification)
                .Include(nl => nl.Contract)
                .Include(nl => nl.Session)
                .Where(nl => nl.Status == status)
                .OrderByDescending(nl => nl.CreatedDate)
                .ToListAsync();
        }

        public async Task<List<NotificationLog>> GetFailedLogsAsync()
        {
            return await _context.NotificationLogs
                .Include(nl => nl.Notification)
                .Include(nl => nl.Contract)
                .Include(nl => nl.Session)
                .Where(nl => nl.Status == "Failed" || nl.Status == "Error")
                .OrderByDescending(nl => nl.CreatedDate)
                .ToListAsync();
        }

        public async Task<List<NotificationLog>> GetLogsByDateRangeAsync(DateTime startDate, DateTime endDate)
        {
            return await _context.NotificationLogs
                .Include(nl => nl.Notification)
                .Include(nl => nl.Contract)
                .Include(nl => nl.Session)
                .Where(nl => nl.CreatedDate >= startDate && nl.CreatedDate <= endDate)
                .OrderByDescending(nl => nl.CreatedDate)
                .ToListAsync();
        }

        public async Task AddAsync(NotificationLog notificationLog)
        {
            _context.NotificationLogs.Add(notificationLog);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(NotificationLog notificationLog)
        {
            _context.NotificationLogs.Update(notificationLog);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(Guid logId)
        {
            var notificationLog = await _context.NotificationLogs.FindAsync(logId);
            if (notificationLog != null)
            {
                _context.NotificationLogs.Remove(notificationLog);
                await _context.SaveChangesAsync();
            }
        }
    }
}