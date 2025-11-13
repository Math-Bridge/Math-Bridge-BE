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
    public class SchoolRepository : ISchoolRepository
    {
        private readonly MathBridgeDbContext _context;

        public SchoolRepository(MathBridgeDbContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        // Basic CRUD Methods
        public async Task AddAsync(School school)
        {
            school.CreatedDate = DateTime.UtcNow.ToLocalTime();
            _context.Schools.Add(school);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(School school)
        {
            school.UpdatedDate = DateTime.UtcNow.ToLocalTime();
            _context.Schools.Update(school);
            await _context.SaveChangesAsync();
        }

        public async Task<School?> GetByIdAsync(Guid schoolId)
        {
            return await _context.Schools
                .Include(s => s.Curriculum)
                .FirstOrDefaultAsync(s => s.SchoolId == schoolId);
        }

        public async Task<List<School>> GetAllAsync()
        {
            return await _context.Schools
                .Include(s => s.Curriculum)
                .OrderBy(s => s.SchoolName)
                .ToListAsync();
        }

        public async Task DeleteAsync(Guid schoolId)
        {
            var school = await _context.Schools
                .Include(s => s.Children)
                .FirstOrDefaultAsync(s => s.SchoolId == schoolId);

            if (school == null)
            {
                throw new KeyNotFoundException($"School with ID {schoolId} not found.");
            }

            if (school.Children.Any())
            {
                throw new InvalidOperationException($"Cannot delete school with ID {schoolId} because it has enrolled children.");
            }

            _context.Schools.Remove(school);
            await _context.SaveChangesAsync();
        }

        // Query Methods
        public async Task<List<School>> GetActiveSchoolsAsync()
        {
            return await _context.Schools
                .Include(s => s.Curriculum)
                .Where(s => s.IsActive == true)
                .OrderBy(s => s.SchoolName)
                .ToListAsync();
        }

        public async Task<List<School>> GetSchoolsByCurriculumIdAsync(Guid curriculumId)
        {
            return await _context.Schools
                .Include(s => s.Curriculum)
                .Where(s => s.CurriculumId == curriculumId)
                .OrderBy(s => s.SchoolName)
                .ToListAsync();
        }

        public async Task<School?> GetByNameAsync(string schoolName)
        {
            return await _context.Schools
                .Include(s => s.Curriculum)
                .FirstOrDefaultAsync(s => s.SchoolName.ToLower() == schoolName.ToLower());
        }

        public async Task<bool> ExistsAsync(Guid schoolId)
        {
            return await _context.Schools.AnyAsync(s => s.SchoolId == schoolId);
        }

        public async Task<bool> ExistsByNameAsync(string schoolName)
        {
            return await _context.Schools.AnyAsync(s => s.SchoolName.ToLower() == schoolName.ToLower());
        }

        // Related Data Methods
        public async Task<Curriculum?> GetCurriculumByIdAsync(Guid curriculumId)
        {
            return await _context.Curricula.FirstOrDefaultAsync(c => c.CurriculumId == curriculumId);
        }

        public async Task<int> GetChildrenCountAsync(Guid schoolId)
        {
            return await _context.Children.CountAsync(c => c.SchoolId == schoolId);
        }

        public async Task<List<Child>> GetChildrenBySchoolIdAsync(Guid schoolId)
        {
            return await _context.Children
                .Include(c => c.Parent)
                .Include(c => c.School)
                .Include(c => c.Center)
                .Where(c => c.SchoolId == schoolId)
                .ToListAsync();
        }
    }
}