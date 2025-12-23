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
    public class WithdrawalRequestRepository : IWithdrawalRequestRepository
    {
        private readonly MathBridgeDbContext _context;

        public WithdrawalRequestRepository(MathBridgeDbContext context)
        {
            _context = context;
        }

        public async Task<List<WithdrawalRequest>> GetAllAsync()
        {
            return await _context.WithdrawalRequests
                .Include(w => w.Parent)
                .Include(w => w.Staff)
                .OrderByDescending(w => w.CreatedDate)
                .ToListAsync();
        }

        public async Task<WithdrawalRequest?> GetByIdAsync(Guid id)
        {
            return await _context.WithdrawalRequests
                .Include(w => w.Parent)
                .Include(w => w.Staff)
                .FirstOrDefaultAsync(w => w.Id == id);
        }

        public async Task<WithdrawalRequest> AddAsync(WithdrawalRequest request)
        {
            _context.WithdrawalRequests.Add(request);
            await _context.SaveChangesAsync();
            return request;
        }

        public async Task<WithdrawalRequest> UpdateAsync(WithdrawalRequest request)
        {
            _context.WithdrawalRequests.Update(request);
            await _context.SaveChangesAsync();
            return request;
        }

        public async Task<List<WithdrawalRequest>> GetByParentIdAsync(Guid parentId)
        {
            return await _context.WithdrawalRequests
                .Where(w => w.ParentId == parentId)
                .OrderByDescending(w => w.CreatedDate)
                .ToListAsync();
        }
    }
}
