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
    public class NotificationTemplateRepository : INotificationTemplateRepository
    {
        private readonly MathBridgeDbContext _context;

        public NotificationTemplateRepository(MathBridgeDbContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        public async Task<NotificationTemplate?> GetByIdAsync(Guid templateId)
        {
            return await _context.NotificationTemplates
                .FirstOrDefaultAsync(nt => nt.TemplateId == templateId);
        }

        public async Task<List<NotificationTemplate>> GetAllAsync()
        {
            return await _context.NotificationTemplates
                .OrderBy(nt => nt.Name)
                .ToListAsync();
        }

        public async Task<NotificationTemplate?> GetByNameAsync(string name)
        {
            return await _context.NotificationTemplates
                .FirstOrDefaultAsync(nt => nt.Name == name);
        }

        public async Task<List<NotificationTemplate>> GetByNotificationTypeAsync(string notificationType)
        {
            return await _context.NotificationTemplates
                .Where(nt => nt.NotificationType == notificationType)
                .OrderBy(nt => nt.Name)
                .ToListAsync();
        }

        public async Task<List<NotificationTemplate>> GetActiveTemplatesAsync()
        {
            return await _context.NotificationTemplates
                .Where(nt => nt.IsActive)
                .OrderBy(nt => nt.Name)
                .ToListAsync();
        }

        public async Task AddAsync(NotificationTemplate template)
        {
            _context.NotificationTemplates.Add(template);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(NotificationTemplate template)
        {
            template.UpdatedDate = DateTime.UtcNow;
            _context.NotificationTemplates.Update(template);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(Guid templateId)
        {
            var template = await _context.NotificationTemplates.FindAsync(templateId);
            if (template != null)
            {
                _context.NotificationTemplates.Remove(template);
                await _context.SaveChangesAsync();
            }
        }
    }
}