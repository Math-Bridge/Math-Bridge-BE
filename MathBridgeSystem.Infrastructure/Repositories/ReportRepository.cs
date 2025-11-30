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
    public class ReportRepository : IReportRepository
    {
        private readonly MathBridgeDbContext _context;

        public ReportRepository(MathBridgeDbContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        public async Task AddAsync(Report report)
        {
            if (report == null)
                throw new ArgumentNullException(nameof(report));
            
            await _context.Reports.AddAsync(report);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(Report report)
        {
            if (report == null)
                throw new ArgumentNullException(nameof(report));

            _context.Reports.Update(report);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(Report report)
        {
            if (report == null)
                throw new ArgumentNullException(nameof(report));

            _context.Reports.Remove(report);
            await _context.SaveChangesAsync();
        }

        public async Task<Report?> GetByIdAsync(Guid reportId)
        {
            return await _context.Reports
                .Include(r => r.Parent)
                .Include(r => r.Tutor)
                .Include(r => r.Contract)
                .FirstOrDefaultAsync(r => r.ReportId == reportId);
        }

        public async Task<List<Report>> GetAllAsync()
        {
            return await _context.Reports
                .Include(r => r.Parent)
                .Include(r => r.Tutor)
                .OrderByDescending(r => r.CreatedDate)
                .ToListAsync();
        }

        public async Task<List<Report>> GetByParentIdAsync(Guid parentId)
        {
            return await _context.Reports
                .Include(r => r.Tutor)
                .Where(r => r.ParentId == parentId)
                .OrderByDescending(r => r.CreatedDate)
                .ToListAsync();
        }

        public async Task<List<Report>> GetByTutorIdAsync(Guid tutorId)
        {
            return await _context.Reports
                .Include(r => r.Parent)
                .Where(r => r.TutorId == tutorId)
                .OrderByDescending(r => r.CreatedDate)
                .ToListAsync();
        }

        public async Task<List<Report>> GetByStatusAsync(string status)
        {
            return await _context.Reports
                .Include(r => r.Parent)
                .Include(r => r.Tutor)
                .Where(r => r.Status.ToLower() == status.ToLower())
                .OrderByDescending(r => r.CreatedDate)
                .ToListAsync();
        }

        public async Task<Report?> GetLatestReportByParentIdAsync(Guid parentId)
        {
            return await _context.Reports
                .Where(r => r.ParentId == parentId)
                .OrderByDescending(r => r.CreatedDate)
                .FirstOrDefaultAsync();
        }
    }
}