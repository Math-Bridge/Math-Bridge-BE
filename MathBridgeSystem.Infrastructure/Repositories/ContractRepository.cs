using MathBridgeSystem.Infrastructure.Data;
using MathBridgeSystem.Domain.Entities;
using MathBridgeSystem.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MathBridgeSystem.Infrastructure.Repositories
{
    public class ContractRepository : IContractRepository
    {
        private readonly MathBridgeDbContext _context;

        public ContractRepository(MathBridgeDbContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        public async Task AddAsync(Contract contract)
        {
            _context.Contracts.Add(contract);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(Contract contract)
        {
            _context.Contracts.Update(contract);
            await _context.SaveChangesAsync();
        }

        public async Task<List<Contract>> GetByParentIdAsync(Guid parentId)
        {
            return await _context.Contracts
                .Include(c => c.Child)
                .Include(c => c.Parent)
                .Include(c => c.MainTutor)
                .Include(c => c.Package)
                .Include(c => c.Center)
                .Where(c => c.ParentId == parentId)
                .ToListAsync();
        }

        public async Task<List<Contract>> GetByChildIdAsync(Guid childId)
        {
            return await _context.Contracts
                .Include(c => c.Child)
                .Include(c => c.Parent)
                .Include(c => c.MainTutor)
                .Include(c => c.Package)
                .Include(c => c.Center)
                .Where(c => c.ChildId == childId)
                .ToListAsync();
        }

        public async Task<Contract?> GetByIdAsync(Guid id)
        {
            return await _context.Contracts
                .Include(c => c.Child)
                .Include(c => c.Parent)
                .Include(c => c.MainTutor)
                .Include(c => c.Package)
                .Include(c => c.Center)
                .FirstOrDefaultAsync(c => c.ContractId == id);
        }

        public async Task<List<Contract>> GetByCenterIdAsync(Guid centerId)
        {
            return await _context.Contracts
                .Include(c => c.Child)
                .Include(c => c.Parent)
                .Include(c => c.MainTutor)
                .Include(c => c.Package)
                .Include(c => c.Center)
                .Where(c => c.CenterId == centerId)
                .ToListAsync();
        }
    }

    public class MathProgramRepository : IMathProgramRepository
    {
        private readonly MathBridgeDbContext _context;

        public MathProgramRepository(MathBridgeDbContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        public async Task<MathProgram> GetByIdAsync(Guid id)
        {
            return await _context.MathPrograms.FirstOrDefaultAsync(p => p.ProgramId == id);
        }

        public async Task<bool> ExistsAsync(Guid id)
        {
            return await _context.MathPrograms.AnyAsync(p => p.ProgramId == id);
        }
    }
}