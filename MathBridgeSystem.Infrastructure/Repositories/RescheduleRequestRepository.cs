﻿using MathBridgeSystem.Domain.Entities;
using MathBridgeSystem.Domain.Interfaces;
using MathBridgeSystem.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading.Tasks;

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
                        .ThenInclude(t => t.Reviews)
                .Include(r => r.Contract)
                    .ThenInclude(c => c.SubstituteTutor2)
                        .ThenInclude(t => t.Reviews)
                .Include(r => r.Parent)
                .Include(r => r.RequestedTutor)
                .FirstOrDefaultAsync(r => r.RequestId == id);
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
    }
}