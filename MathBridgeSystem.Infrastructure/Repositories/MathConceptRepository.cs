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
    public class MathConceptRepository : IMathConceptRepository
    {
        private readonly MathBridgeDbContext _context;

        public MathConceptRepository(MathBridgeDbContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        public async Task AddAsync(MathConcept mathConcept)
        {
            try
            {
                if (mathConcept == null)
                    throw new ArgumentNullException(nameof(mathConcept));

                await _context.MathConcepts.AddAsync(mathConcept);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException ex)
            {
                throw new InvalidOperationException("Error adding math concept to database.", ex);
            }
        }

        public async Task UpdateAsync(MathConcept mathConcept)
        {
            try
            {
                if (mathConcept == null)
                    throw new ArgumentNullException(nameof(mathConcept));

                _context.MathConcepts.Update(mathConcept);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException ex)
            {
                throw new InvalidOperationException("Error updating math concept in database.", ex);
            }
        }

        public async Task DeleteAsync(Guid id)
        {
            try
            {
                var mathConcept = await _context.MathConcepts.FindAsync(id);
                if (mathConcept != null)
                {
                    _context.MathConcepts.Remove(mathConcept);
                    await _context.SaveChangesAsync();
                }
            }
            catch (DbUpdateException ex)
            {
                throw new InvalidOperationException("Error deleting math concept from database.", ex);
            }
        }

        public async Task<MathConcept?> GetByIdAsync(Guid id)
        {
            try
            {
                return await _context.MathConcepts
                    .Include(mc => mc.Units)
                        .ThenInclude(u => u.Curriculum)
                    .FirstOrDefaultAsync(mc => mc.ConceptId == id);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Error retrieving math concept from database.", ex);
            }
        }

        public async Task<List<MathConcept>> GetAllAsync()
        {
            try
            {
                return await _context.MathConcepts
                    .Include(mc => mc.Units)
                        .ThenInclude(u => u.Curriculum)
                    .OrderBy(mc => mc.Category)
                    .ThenBy(mc => mc.Name)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Error retrieving all math concepts from database.", ex);
            }
        }

        public async Task<List<MathConcept>> GetByUnitIdAsync(Guid unitId)
        {
            try
            {
                return await _context.MathConcepts
                    .Include(mc => mc.Units)
                    .Where(mc => mc.Units.Any(u => u.UnitId == unitId))
                    .OrderBy(mc => mc.Category)
                    .ThenBy(mc => mc.Name)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Error retrieving math concepts by unit ID from database.", ex);
            }
        }

        public async Task<MathConcept?> GetByNameAsync(string name)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(name))
                    throw new ArgumentException("Name cannot be null or empty.", nameof(name));

                return await _context.MathConcepts
                    .Include(mc => mc.Units)
                    .FirstOrDefaultAsync(mc => mc.Name.ToLower() == name.ToLower());
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Error retrieving math concept by name from database.", ex);
            }
        }

        public async Task<bool> ExistsAsync(Guid id)
        {
            try
            {
                return await _context.MathConcepts.AnyAsync(mc => mc.ConceptId == id);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Error checking if math concept exists.", ex);
            }
        }

        public async Task<bool> ExistsByNameAsync(string name)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(name))
                    return false;

                return await _context.MathConcepts.AnyAsync(mc => mc.Name.ToLower() == name.ToLower());
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Error checking if math concept exists by name.", ex);
            }
        }

        public async Task<List<MathConcept>> GetByCategoryAsync(string category)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(category))
                    throw new ArgumentException("Category cannot be null or empty.", nameof(category));

                return await _context.MathConcepts
                    .Include(mc => mc.Units)
                        .ThenInclude(u => u.Curriculum)
                    .Where(mc => mc.Category.ToLower() == category.ToLower())
                    .OrderBy(mc => mc.Name)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Error retrieving math concepts by category from database.", ex);
            }
        }

        public async Task<bool> IsMathConceptLinkedToUnitsAsync(Guid conceptId)
        {
            try
            {
                return await _context.MathConcepts
                    .Where(mc => mc.ConceptId == conceptId)
                    .AnyAsync(mc => mc.Units.Any());
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Error checking if math concept is linked to units.", ex);
            }
        }
    }
}
