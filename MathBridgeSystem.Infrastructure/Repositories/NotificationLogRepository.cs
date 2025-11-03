using MathBridgeSystem.Infrastructure.Data;
using MathBridgeSystem.Domain.Entities;
using MathBridgeSystem.Domain.Interfaces;
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

        public async Task AddAsync(NotificationLog log)
        {
            _context.NotificationLogs.Add(log);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(NotificationLog log)
        {
            _context.NotificationLogs.Update(log);
            await _context.SaveChangesAsync();
        }

        public async Task<NotificationLog?> GetByIdAsync(Guid logId)
        {
            return await _context.NotificationLogs
                .Include(l => l.Notification)
                .Include(l => l.Contract)
                .Include(l => l.Session)
                .FirstOrDefaultAsync(l => l.LogId == logId);
        }

        public async Task<List<NotificationLog>> GetByNotificationIdAsync(Guid notificationId)
        {
            return await _context.NotificationLogs
                .Include(l => l.Notification)
                .Include(l => l.Contract)
                .Include(l => l.Session)
                .Where(l => l.NotificationId == notificationId)
                .OrderByDescending(l => l.CreatedDate)
                .ToListAsync();
        }

        public async Task<List<NotificationLog>> GetByContractIdAsync(Guid contractId)
        {
            return await _context.NotificationLogs
                .Include(l => l.Notification)
                .Include(l => l.Contract)
                .Include(l => l.Session)
                .Where(l => l.ContractId == contractId)
                .OrderByDescending(l => l.CreatedDate)
                .ToListAsync();
        }

        public async Task<List<NotificationLog>> GetBySessionIdAsync(Guid sessionId)
        {
            return await _context.NotificationLogs
                .Include(l => l.Notification)
                .Include(l => l.Contract)
                .Include(l => l.Session)
                .Where(l => l.SessionId == sessionId)
                .OrderByDescending(l => l.CreatedDate)
                .ToListAsync();
        }

        public async Task<List<NotificationLog>> GetFailedLogsAsync(int pageNumber, int pageSize)
        {
            return await _context.NotificationLogs
                .Include(l => l.Notification)
                .Include(l => l.Contract)
                .Include(l => l.Session)
                .Where(l => l.Status == "Failed")
                .OrderByDescending(l => l.CreatedDate)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }

        public async Task<int> GetFailedCountAsync()
        {
            return await _context.NotificationLogs
                .Where(l => l.Status == "Failed")
                .CountAsync();
        }
    }
}