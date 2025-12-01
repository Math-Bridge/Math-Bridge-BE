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

        private IQueryable<Session> WithFullIncludes()
        {
            return _context.Sessions
                .Include(s => s.Contract)
                    .ThenInclude(c => c.Parent)
                .Include(s => s.Contract)
                    .ThenInclude(c => c.Child)
                .Include(s => s.Contract)
                    .ThenInclude(c => c.Package)
                .Include(s => s.Tutor);
        }

        public async Task AddRangeAsync(IEnumerable<Session> sessions)
        {
            _context.Sessions.AddRange(sessions);
            await _context.SaveChangesAsync();
        }

        public async Task<Session?> GetByIdAsync(Guid bookingId)
        {
            return await WithFullIncludes()
                .FirstOrDefaultAsync(s => s.BookingId == bookingId);
        }

        public async Task UpdateAsync(Session session)
        {
            session.UpdatedAt = DateTime.UtcNow.ToLocalTime();
            _context.Sessions.Update(session);
            await _context.SaveChangesAsync();
        }

        public async Task<bool> IsTutorAvailableAsync(Guid tutorId, DateOnly date, DateTime startTime, DateTime endTime)
        {
            var start = new DateTime(date.Year, date.Month, date.Day) + startTime.TimeOfDay;
            var end = new DateTime(date.Year, date.Month, date.Day) + endTime.TimeOfDay;

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
            return await WithFullIncludes()
                .Where(s => s.Contract.ParentId == parentId)
                .OrderBy(s => s.SessionDate)
                .ThenBy(s => s.StartTime)
                .ToListAsync();
        }

        public async Task<List<Session>> GetByContractIdAsync(Guid contractId)
        {
            return await WithFullIncludes()
                .Where(s => s.ContractId == contractId)
                .ToListAsync();
        }

        public async Task<List<Session>> GetSessionsInTimeRangeAsync(DateTime startTime, DateTime endTime)
        {
            return await WithFullIncludes()
                .Where(s => s.StartTime >= startTime && s.StartTime <= endTime && s.Status == "scheduled")
                .OrderBy(s => s.StartTime)
                .ToListAsync();
        }
        public async Task<List<Session>> GetAllSessionsInTimeRangeAsync(DateTime startTime, DateTime endTime)
        {
            return await WithFullIncludes()
                .Where(s => s.StartTime >= startTime && s.StartTime <= endTime)
                .OrderBy(s => s.StartTime)
                .ToListAsync();
        }
        public async Task<List<Session>> GetByChildIdAsync(Guid childId, Guid parentId)
        {
            return await WithFullIncludes()
                .Where(s => s.Contract.ChildId == childId && s.Contract.ParentId == parentId)
                .OrderBy(s => s.SessionDate)
                .ThenBy(s => s.StartTime)
                .ToListAsync();
        }

        // MỚI: LẤY THEO TUTOR ĐANG DẠY (Session.TutorId)
        public async Task<List<Session>> GetByTutorIdAsync(Guid tutorId)
        {
            return await WithFullIncludes()
                .Where(s => s.TutorId == tutorId)
                .OrderBy(s => s.SessionDate)
                .ThenBy(s => s.StartTime)
                .ToListAsync();
        }
        public async Task<List<Session>> GetUpcomingSessionsByContractIdAsync(Guid contractId, DateOnly fromDate)
        {
            return await WithFullIncludes()
                .Where(s => s.ContractId == contractId
                         && s.SessionDate >= fromDate
                         && s.Status == "scheduled")
                .ToListAsync();
        }
        public async Task UpdateRangeAsync(IEnumerable<Session> sessions)
        {
            foreach (var session in sessions)
            {
                session.UpdatedAt = DateTime.UtcNow.ToLocalTime();
            }
            _context.Sessions.UpdateRange(sessions);
            await _context.SaveChangesAsync();
        }
    }
}