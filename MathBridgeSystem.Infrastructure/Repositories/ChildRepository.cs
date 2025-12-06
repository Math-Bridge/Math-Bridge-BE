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
    public class ChildRepository : IChildRepository
    {
        private readonly MathBridgeDbContext _context;

        public ChildRepository(MathBridgeDbContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        public async Task AddAsync(Child child)
        {
            _context.Children.Add(child);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(Child child)
        {
            _context.Children.Update(child);
            await _context.SaveChangesAsync();
        }

        public async Task<Child> GetByIdAsync(Guid id)
        {
            return await _context.Children
                .Include(c => c.Parent)
                .Include(c => c.Center)
                .Include(c => c.ContractChildren)
                .Include(c=> c.ContractSecondChildren)
                .Include(c => c.School)
                .FirstOrDefaultAsync(c => c.ChildId == id);
        }

        public async Task<List<Child>> GetByParentIdAsync(Guid parentId)
        {
            return await _context.Children
                .Include(c => c.Parent)
                                .Include(c => c.Center)
                .Include(c => c.ContractChildren)
                .Include(c=> c.ContractSecondChildren)
                .Include(c => c.School)
                .Where(c => c.ParentId == parentId)
                .ToListAsync();
        }

        public async Task<List<Child>> GetAllAsync()
        {
            return await _context.Children
                .Include(c => c.Parent)
                                .Include(c => c.Center)
                .Include(c => c.ContractChildren)
                .Include(c=> c.ContractSecondChildren)
                .Include(c => c.School)
                .ToListAsync();
        }

        public async Task<List<Contract>> GetContractsByChildIdAsync(Guid childId)
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

        public async Task<Center?> GetCenterByIdAsync(Guid centerId)
        {
            return await _context.Centers.FirstOrDefaultAsync(c => c.CenterId == centerId);
        }
    }
}