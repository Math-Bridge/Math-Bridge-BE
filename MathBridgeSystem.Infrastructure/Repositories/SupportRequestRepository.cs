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
    public class SupportRequestRepository : ISupportRequestRepository
    {
        private readonly MathBridgeDbContext _context;

        public SupportRequestRepository(MathBridgeDbContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        public async Task AddAsync(SupportRequest supportRequest)
        {
            await _context.SupportRequests.AddAsync(supportRequest);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(SupportRequest supportRequest)
        {
            _context.SupportRequests.Update(supportRequest);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(Guid id)
        {
            var supportRequest = await _context.SupportRequests.FindAsync(id);
            if (supportRequest != null)
            {
                _context.SupportRequests.Remove(supportRequest);
                await _context.SaveChangesAsync();
            }
        }

        public async Task<SupportRequest?> GetByIdAsync(Guid id)
        {
            return await _context.SupportRequests
                .Include(sr => sr.User)
                .Include(sr => sr.AssignedToUser)
                .FirstOrDefaultAsync(sr => sr.RequestId == id);
        }

        public async Task<List<SupportRequest>> GetAllAsync()
        {
            return await _context.SupportRequests
                .Include(sr => sr.User)
                .Include(sr => sr.AssignedToUser)
                .OrderByDescending(sr => sr.CreatedDate)
                .ToListAsync();
        }

        public async Task<List<SupportRequest>> GetByUserIdAsync(Guid userId)
        {
            return await _context.SupportRequests
                .Include(sr => sr.AssignedToUser)
                .Where(sr => sr.UserId == userId)
                .OrderByDescending(sr => sr.CreatedDate)
                .ToListAsync();
        }

        public async Task<List<SupportRequest>> GetByStatusAsync(string status)
        {
            return await _context.SupportRequests
                .Include(sr => sr.User)
                .Include(sr => sr.AssignedToUser)
                .Where(sr => sr.Status.ToLower() == status.ToLower())
                .OrderByDescending(sr => sr.CreatedDate)
                .ToListAsync();
        }

        public async Task<List<SupportRequest>> GetByCategoryAsync(string category)
        {
            return await _context.SupportRequests
                .Include(sr => sr.User)
                .Include(sr => sr.AssignedToUser)
                .Where(sr => sr.Category.ToLower() == category.ToLower())
                .OrderByDescending(sr => sr.CreatedDate)
                .ToListAsync();
        }

        public async Task<List<SupportRequest>> GetByAssignedUserIdAsync(Guid assignedUserId)
        {
            return await _context.SupportRequests
                .Include(sr => sr.User)
                .Where(sr => sr.AssignedToUserId == assignedUserId)
                .OrderByDescending(sr => sr.CreatedDate)
                .ToListAsync();
        }

        public async Task<bool> ExistsAsync(Guid id)
        {
            return await _context.SupportRequests.AnyAsync(sr => sr.RequestId == id);
        }
    }
}