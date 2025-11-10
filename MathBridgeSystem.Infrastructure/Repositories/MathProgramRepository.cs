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
    public class MathProgramRepository : IMathProgramRepository
    {
        private readonly MathBridgeDbContext _context;

        public MathProgramRepository(MathBridgeDbContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        public async Task AddAsync(MathProgram mathProgram)
        {
            await _context.MathPrograms.AddAsync(mathProgram);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(MathProgram mathProgram)
        {
            _context.MathPrograms.Update(mathProgram);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(Guid id)
        {
            var mathProgram = await _context.MathPrograms.FindAsync(id);
            if (mathProgram != null)
            {
                _context.MathPrograms.Remove(mathProgram);
                await _context.SaveChangesAsync();
            }
        }

        public async Task<MathProgram?> GetByIdAsync(Guid id)
        {
            return await _context.MathPrograms
                .Include(mp => mp.PaymentPackages)
                .Include(mp => mp.TestResults)
                .FirstOrDefaultAsync(mp => mp.ProgramId == id);
        }

        public async Task<List<MathProgram>> GetAllAsync()
        {
            return await _context.MathPrograms
                .OrderBy(mp => mp.ProgramName)
                .ToListAsync();
        }

        public async Task<MathProgram?> GetByNameAsync(string programName)
        {
            return await _context.MathPrograms
                .FirstOrDefaultAsync(mp => mp.ProgramName.ToLower() == programName.ToLower());
        }

        public async Task<bool> ExistsAsync(Guid id)
        {
            return await _context.MathPrograms.AnyAsync(mp => mp.ProgramId == id);
        }

        public async Task<bool> ExistsByNameAsync(string programName)
        {
            return await _context.MathPrograms.AnyAsync(mp => mp.ProgramName.ToLower() == programName.ToLower());
        }
    }
}