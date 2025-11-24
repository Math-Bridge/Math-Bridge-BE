﻿﻿using MathBridgeSystem.Domain.Entities;
using MathBridgeSystem.Domain.Interfaces;
using MathBridgeSystem.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace MathBridgeSystem.Infrastructure.Repositories
{
    public class RescheduleRequestRepository : IRescheduleRequestRepository
    {
        private readonly MathBridgeDbContext _context;

        public RescheduleRequestRepository(MathBridgeDbContext context)
        {
            _context = context;
        }

        public async Task AddAsync(RescheduleRequest entity)
        {
            _context.RescheduleRequests.Add(entity);
            await _context.SaveChangesAsync();
        }

        public async Task<RescheduleRequest?> GetByIdWithDetailsAsync(Guid id)
        {
            return await _context.RescheduleRequests
                .Include(r => r.Booking)
                    .ThenInclude(s => s.Contract)
                        .ThenInclude(c => c.Package)
                .Include(r => r.Booking)
                    .ThenInclude(s => s.Tutor)
                .Include(r => r.Contract)
                    .ThenInclude(c => c.SubstituteTutor1)
                        .ThenInclude(t => t.FinalFeedbacks)
                .Include(r => r.Contract)
                    .ThenInclude(c => c.SubstituteTutor2)
                        .ThenInclude(t => t.FinalFeedbacks)
                .Include(r => r.Parent)
                .Include(r => r.RequestedTutor)
                .Include(r => r.Staff)
                .FirstOrDefaultAsync(r => r.RequestId == id);
        }

        public async Task<IEnumerable<RescheduleRequest>> GetAllAsync()
        {
            return await _context.RescheduleRequests
                .Include(r => r.Booking)
                    .ThenInclude(s => s.Tutor)
                .Include(r => r.Parent)
                .Include(r => r.RequestedTutor)
                .Include(r => r.Staff)
                .OrderByDescending(r => r.CreatedDate)
                .ToListAsync();
        }

        public async Task<IEnumerable<RescheduleRequest>> GetByParentIdAsync(Guid parentId)
        {
            return await _context.RescheduleRequests
                .Include(r => r.Booking)
                    .ThenInclude(s => s.Tutor)
                .Include(r => r.Parent)
                .Include(r => r.RequestedTutor)
                .Include(r => r.Staff)
                .Where(r => r.ParentId == parentId)
                .OrderByDescending(r => r.CreatedDate)
                .ToListAsync();
        }

        public async Task UpdateAsync(RescheduleRequest entity)
        {
            _context.RescheduleRequests.Update(entity);
            await _context.SaveChangesAsync();
        }

        public async Task<bool> HasPendingRequestForBookingAsync(Guid bookingId)
        {
            return await _context.RescheduleRequests
                .AnyAsync(r => r.BookingId == bookingId && r.Status == "pending");
        }
        public async Task<RescheduleRequest?> GetPendingRequestForBookingAsync(Guid bookingId)
        {
            return await _context.RescheduleRequests
                .FirstOrDefaultAsync(r => r.BookingId == bookingId && r.Status == "pending");
        }
    }
}