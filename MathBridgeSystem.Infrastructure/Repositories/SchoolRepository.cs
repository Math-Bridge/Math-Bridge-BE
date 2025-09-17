using MathBridge.Domain.Entities;
using MathBridgeSystem.Domain.Interfaces;
using MathBridge.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using MathBridge.Infrastructure.Data;

namespace MathBridgeSystem.Infrastructure.Repositories
{
    public class SchoolRepository : ISchoolRepository
    {
        private readonly MathBridgeDbContext _context;

        public SchoolRepository(MathBridgeDbContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        public async Task AddAsync(School school)
        {
            await _context.Schools.AddAsync(school);
            await _context.SaveChangesAsync();
        }

        public async Task<School?> GetByIdAsync(Guid id)
        {
            return await _context.Schools.FindAsync(id);
        }

        public async Task<IEnumerable<School>> GetAllAsync()
        {
            return await _context.Schools.ToListAsync();
        }

        public async Task UpdateAsync(School school)
        {
            _context.Schools.Update(school);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(Guid id)
        {
            var school = await GetByIdAsync(id);
            if (school != null)
            {
                _context.Schools.Remove(school);
                await _context.SaveChangesAsync();
            }
        }

        public async Task<bool> ExistsAsync(Guid id)
        {
            return await _context.Schools.AnyAsync(s => s.SchoolId == id);
        }
    }
}