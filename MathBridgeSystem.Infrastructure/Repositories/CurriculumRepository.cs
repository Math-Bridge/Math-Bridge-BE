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
    public class CurriculumRepository : ICurriculumRepository
    {
        private readonly MathBridgeDbContext _context;

        public CurriculumRepository(MathBridgeDbContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        // Basic CRUD Methods
        public async Task AddAsync(Curriculum curriculum)
        {
            curriculum.CreatedDate = DateTime.UtcNow;
            _context.Curricula.Add(curriculum);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(Curriculum curriculum)
        {
            curriculum.UpdatedDate = DateTime.UtcNow;
            _context.Curricula.Update(curriculum);
            await _context.SaveChangesAsync();
        }

        public async Task<Curriculum?> GetByIdAsync(Guid curriculumId)
        {
            return await _context.Curricula
                .FirstOrDefaultAsync(c => c.CurriculumId == curriculumId);
        }

        public async Task<List<Curriculum>> GetAllAsync()
        {
            return await _context.Curricula
                .OrderBy(c => c.CurriculumCode)
                .ToListAsync();
        }

        public async Task DeleteAsync(Guid curriculumId)
        {
            var curriculum = await _context.Curricula
                .Include(c => c.Schools)
                .Include(c => c.PaymentPackages)
                .FirstOrDefaultAsync(c => c.CurriculumId == curriculumId);

            if (curriculum == null)
            {
                throw new KeyNotFoundException($"Curriculum with ID {curriculumId} not found.");
            }

            if (curriculum.Schools.Any())
            {
                throw new InvalidOperationException($"Cannot delete curriculum with ID {curriculumId} because it has associated schools.");
            }

            if (curriculum.PaymentPackages.Any())
            {
                throw new InvalidOperationException($"Cannot delete curriculum with ID {curriculumId} because it has associated payment packages.");
            }

            _context.Curricula.Remove(curriculum);
            await _context.SaveChangesAsync();
        }

        // Query Methods
        public async Task<List<Curriculum>> GetActiveAsync()
        {
            return await _context.Curricula
                .Where(c => c.IsActive)
                .OrderBy(c => c.CurriculumCode)
                .ToListAsync();
        }

        public async Task<Curriculum?> GetByCodeAsync(string code)
        {
            return await _context.Curricula
                .FirstOrDefaultAsync(c => c.CurriculumCode.ToLower() == code.ToLower());
        }

        public async Task<bool> ExistsByCodeAsync(string code)
        {
            return await _context.Curricula.AnyAsync(c => c.CurriculumCode.ToLower() == code.ToLower());
        }

        // Related Data Methods
        public async Task<int> GetSchoolsCountAsync(Guid curriculumId)
        {
            return await _context.Schools.CountAsync(s => s.CurriculumId == curriculumId);
        }

        public async Task<int> GetPackagesCountAsync(Guid curriculumId)
        {
            return await _context.PaymentPackages.CountAsync(p => p.CurriculumId == curriculumId);
        }

        public async Task<List<School>> GetSchoolsByCurriculumIdAsync(Guid curriculumId)
        {
            return await _context.Schools
                .Include(s => s.Curriculum)
                .Where(s => s.CurriculumId == curriculumId)
                .ToListAsync();
        }
    }
}