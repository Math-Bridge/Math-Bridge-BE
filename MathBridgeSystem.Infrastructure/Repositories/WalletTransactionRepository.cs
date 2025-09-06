using MathBridge.Domain.Entities;
using MathBridge.Domain.Interfaces;
using MathBridge.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace MathBridge.Infrastructure.Repositories
{
    public class WalletTransactionRepository : IWalletTransactionRepository
    {
        private readonly MathBridgeDbContext _context;

        public WalletTransactionRepository(MathBridgeDbContext context)
        {
            _context = context;
        }

        public async Task<List<WalletTransaction>> GetByParentIdAsync(Guid parentId)
        {
            return await _context.WalletTransactions
                .Where(wt => wt.ParentId == parentId)
                .ToListAsync();
        }
    }
}