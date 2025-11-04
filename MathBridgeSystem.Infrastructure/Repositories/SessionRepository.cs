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
    public class SessionRepository : ISessionRepository
    {
        private readonly MathBridgeDbContext _context;

        public SessionRepository(MathBridgeDbContext context)
        {
            _context = context;
        }

        public async Task AddRangeAsync(IEnumerable<Session> sessions)
        {
            _context.Sessions.AddRange(sessions);
            await _context.SaveChangesAsync();
        }

        public async Task<Session?> GetByIdAsync(Guid bookingId)
        {
            return await _context.Sessions
                .Include(s => s.Contract)
                    .ThenInclude(c => c.Parent)
                .Include(s => s.Contract)
                    .ThenInclude(c => c.Package)
                .Include(s => s.Tutor)
                .FirstOrDefaultAsync(s => s.BookingId == bookingId);
        }

        public async Task UpdateAsync(Session session)
        {
            session.UpdatedAt = DateTime.UtcNow;
            _context.Sessions.Update(session);
            await _context.SaveChangesAsync();
        }

        public async Task<bool> IsTutorAvailableAsync(Guid tutorId, DateOnly date, TimeOnly startTime, TimeOnly endTime)
        {
            var start = new DateTime(date.Year, date.Month, date.Day) + startTime.ToTimeSpan();
            var end = new DateTime(date.Year, date.Month, date.Day) + endTime.ToTimeSpan();

            return !await _context.Sessions.AnyAsync(s =>
                s.TutorId == tutorId &&
                s.SessionDate == date &&
                s.Status == "scheduled" &&
                ((s.StartTime >= start && s.StartTime < end) ||
                 (s.EndTime > start && s.EndTime <= end) ||
                 (s.StartTime <= start && s.EndTime >= end)));
        }

        public async Task<List<Session>> GetByParentIdAsync(Guid parentId)
        {
            return await _context.Sessions
                .Include(s => s.Contract)
                    .ThenInclude(c => c.Parent)
                .Include(s => s.Tutor)
                .Where(s => s.Contract.ParentId == parentId)
                .OrderBy(s => s.SessionDate)
                .ThenBy(s => s.StartTime)
                .ToListAsync();
        }
        public async Task<List<Session>> GetByContractIdAsync(Guid contractId)
        {
            return await _context.Sessions
                .Where(s => s.ContractId == contractId)
                .ToListAsync();
        }
    }
}