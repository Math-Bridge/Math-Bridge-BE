using MathBridgeSystem.Infrastructure.Data;
using MathBridgeSystem.Domain.Entities;
using MathBridgeSystem.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading.Tasks;

namespace MathBridgeSystem.Infrastructure.Repositories
{
    public class NotificationPreferenceRepository : INotificationPreferenceRepository
    {
        private readonly MathBridgeDbContext _context;

        public NotificationPreferenceRepository(MathBridgeDbContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        public async Task AddAsync(NotificationPreference preference)
        {
            _context.NotificationPreferences.Add(preference);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(NotificationPreference preference)
        {
            _context.NotificationPreferences.Update(preference);
            await _context.SaveChangesAsync();
        }

        public async Task<NotificationPreference?> GetByUserIdAsync(Guid userId)
        {
            return await _context.NotificationPreferences
                .Include(p => p.User)
                .FirstOrDefaultAsync(p => p.UserId == userId);
        }

        public async Task<bool> ExistsByUserIdAsync(Guid userId)
        {
            return await _context.NotificationPreferences
                .AnyAsync(p => p.UserId == userId);
        }
    }
}