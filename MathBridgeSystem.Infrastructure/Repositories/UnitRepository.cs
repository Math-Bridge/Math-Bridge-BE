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
    public class UnitRepository : IUnitRepository
    {
        private readonly MathBridgeDbContext _context;

        public UnitRepository(MathBridgeDbContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        public async Task AddAsync(Unit unit)
        {
            await _context.Units.AddAsync(unit);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(Unit unit)
        {
            _context.Units.Update(unit);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(Guid id)
        {
            var unit = await _context.Units.FindAsync(id);
            if (unit != null)
            {
                _context.Units.Remove(unit);
                await _context.SaveChangesAsync();
            }
        }

        public async Task<Unit?> GetByIdAsync(Guid id)
        {
            return await _context.Units
                .Include(u => u.Curriculum)
                .Include(u => u.CreatedByNavigation)
                .Include(u => u.UpdatedByNavigation)
                .FirstOrDefaultAsync(u => u.UnitId == id);
        }

        public async Task<List<Unit>> GetAllAsync()
        {
            return await _context.Units
                .Include(u => u.Curriculum)
                .OrderBy(u => u.Curriculum.CurriculumName)
                .ThenBy(u => u.UnitOrder)
                .ToListAsync();
        }

        public async Task<List<Unit>> GetByCurriculumIdAsync(Guid curriculumId)
        {
            return await _context.Units
                .Where(u => u.CurriculumId == curriculumId)
                .OrderBy(u => u.UnitOrder)
                .ToListAsync();
        }

        public async Task<Unit?> GetByNameAsync(string unitName)
        {
            return await _context.Units
                .Include(u => u.Curriculum)
                .FirstOrDefaultAsync(u => u.UnitName.ToLower() == unitName.ToLower());
        }

        public async Task<bool> ExistsAsync(Guid id)
        {
            return await _context.Units.AnyAsync(u => u.UnitId == id);
        }

        public async Task<bool> ExistsByNameAsync(string unitName, Guid curriculumId)
        {
            return await _context.Units.AnyAsync(u => 
                u.UnitName.ToLower() == unitName.ToLower() && 
                u.CurriculumId == curriculumId);
        }

        public async Task<int> GetMaxUnitOrderAsync(Guid curriculumId)
        {
            var maxOrder = await _context.Units
                .Where(u => u.CurriculumId == curriculumId)
                .MaxAsync(u => (int?)u.UnitOrder);
            
            return maxOrder ?? 0;
        }

        public async Task<List<Unit>> GetByContractIdAsync(Guid contractId)
        {
            return await _context.Units
                .Include(u => u.Curriculum)
                .Where(u => u.Curriculum.PaymentPackages.Any(p => p.Contracts.Any(c => c.ContractId == contractId)))
                .OrderBy(u => u.UnitOrder)
                .ToListAsync();
        }
    }
}