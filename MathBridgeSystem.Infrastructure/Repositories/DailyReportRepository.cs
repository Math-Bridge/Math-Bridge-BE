using MathBridgeSystem.Domain.Entities;
using MathBridgeSystem.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MathBridgeSystem.Infrastructure.Data;

namespace MathBridgeSystem.Infrastructure.Repositories
{
    public class DailyReportRepository : IDailyReportRepository
    {
        private readonly MathBridgeDbContext _context;

        public DailyReportRepository(MathBridgeDbContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        public async Task<DailyReport> GetByIdAsync(Guid reportId)
        {
            return await _context.DailyReports
                .Include(d => d.Child)
                .Include(d => d.Tutor)
                .Include(d => d.Booking)
                .Include(d => d.Unit)
                .Include(d => d.Test)
                .FirstOrDefaultAsync(d => d.ReportId == reportId);
        }

        public async Task<IEnumerable<DailyReport>> GetByTutorIdAsync(Guid tutorId)
        {
            return await _context.DailyReports
                .Include(d => d.Child)
                .Include(d => d.Tutor)
                .Include(d => d.Booking)
                .Include(d => d.Unit)
                .Include(d => d.Test)
                .Where(d => d.TutorId == tutorId)
                .OrderByDescending(d => d.CreatedDate)
                .ToListAsync();
        }

        public async Task<IEnumerable<DailyReport>> GetByChildIdAsync(Guid childId)
        {
            return await _context.DailyReports
                .Include(d => d.Child)
                .Include(d => d.Tutor)
                .Include(d => d.Booking)
                .Include(d => d.Unit)
                .Include(d => d.Test)
                .Where(d => d.ChildId == childId)
                .OrderByDescending(d => d.CreatedDate)
                .ToListAsync();
        }

        public async Task<IEnumerable<DailyReport>> GetByBookingIdAsync(Guid bookingId)
        {
            return await _context.DailyReports
                .Include(d => d.Child)
                .Include(d => d.Tutor)
                .Include(d => d.Booking)
                .Include(d => d.Unit)
                .Include(d => d.Test)
                .Where(d => d.BookingId == bookingId)
                .OrderByDescending(d => d.CreatedDate)
                .ToListAsync();
        }

        public async Task<DailyReport> AddAsync(DailyReport dailyReport)
        {
            _context.DailyReports.Add(dailyReport);
            await _context.SaveChangesAsync();
            return dailyReport;
        }

        public async Task<DailyReport> UpdateAsync(DailyReport dailyReport)
        {
            _context.DailyReports.Update(dailyReport);
            await _context.SaveChangesAsync();
            return dailyReport;
        }

        public async Task<bool> DeleteAsync(Guid reportId)
        {
            var dailyReport = await _context.DailyReports.FindAsync(reportId);
            if (dailyReport == null)
                return false;

            _context.DailyReports.Remove(dailyReport);
            await _context.SaveChangesAsync();
            return true;
        }
    }
}

