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
    public class NotificationRepository : INotificationRepository
    {
        private readonly MathBridgeDbContext _context;

        public NotificationRepository(MathBridgeDbContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        public async Task AddAsync(Notification notification)
        {
            _context.Notifications.Add(notification);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(Notification notification)
        {
            _context.Notifications.Update(notification);
            await _context.SaveChangesAsync();
        }

        public async Task<Notification?> GetByIdAsync(Guid id)
        {
            return await _context.Notifications
                .Include(n => n.User)
                .Include(n => n.Contract)
                .Include(n => n.Booking)
                .FirstOrDefaultAsync(n => n.NotificationId == id);
        }

        public async Task<List<Notification>> GetByUserIdAsync(Guid userId)
        {
            return await _context.Notifications
                .Include(n => n.User)
                .Include(n => n.Contract)
                .Include(n => n.Booking)
                .Where(n => n.UserId == userId)
                .OrderByDescending(n => n.CreatedDate)
                .ToListAsync();
        }

        public async Task<List<Notification>> GetUnreadByUserIdAsync(Guid userId)
        {
            return await _context.Notifications
                .Include(n => n.User)
                .Include(n => n.Contract)
                .Include(n => n.Booking)
                .Where(n => n.UserId == userId && n.Status != "Read")
                .OrderByDescending(n => n.CreatedDate)
                .ToListAsync();
        }

        public async Task<List<Notification>> GetByContractIdAsync(Guid contractId)
        {
            return await _context.Notifications
                .Include(n => n.User)
                .Include(n => n.Contract)
                .Include(n => n.Booking)
                .Where(n => n.ContractId == contractId)
                .OrderByDescending(n => n.CreatedDate)
                .ToListAsync();
        }

        public async Task<List<Notification>> GetPaginatedByUserIdAsync(Guid userId, int pageNumber, int pageSize)
        {
            return await _context.Notifications
                .Include(n => n.User)
                .Include(n => n.Contract)
                .Include(n => n.Booking)
                .Where(n => n.UserId == userId)
                .OrderByDescending(n => n.CreatedDate)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }

        public async Task<int> GetUnreadCountAsync(Guid userId)
        {
            return await _context.Notifications
                .Where(n => n.UserId == userId && n.Status != "Read")
                .CountAsync();
        }

        public async Task MarkAsReadAsync(Guid notificationId)
        {
            var notification = await _context.Notifications.FindAsync(notificationId);
            if (notification != null)
            {
                notification.Status = "Read";
                await _context.SaveChangesAsync();
            }
        }

        public async Task MarkAllAsReadAsync(Guid userId)
        {
            var notifications = await _context.Notifications
                .Where(n => n.UserId == userId && n.Status != "Read")
                .ToListAsync();

            foreach (var notification in notifications)
            {
                notification.Status = "Read";
            }

            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(Guid notificationId)
        {
            var notification = await _context.Notifications.FindAsync(notificationId);
            if (notification != null)
            {
                _context.Notifications.Remove(notification);
                await _context.SaveChangesAsync();
            }
        }

        public async Task DeleteAllAsync(Guid userId)
        {
            var notifications = await _context.Notifications
                .Where(n => n.UserId == userId)
                .ToListAsync();

            _context.Notifications.RemoveRange(notifications);
            await _context.SaveChangesAsync();
        }
    }
}