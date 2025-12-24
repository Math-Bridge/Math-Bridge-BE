using MathBridgeSystem.Domain.Entities;
using MathBridgeSystem.Domain.Interfaces;
using MathBridgeSystem.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace MathBridgeSystem.Infrastructure.Repositories
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
                .OrderByDescending(wt => wt.TransactionDate)
                .ToListAsync();
        }

        public async Task<List<WalletTransaction>> GetByContractIdAsync(Guid contractId)
        {
            return await _context.WalletTransactions
                .Where(wt => wt.ContractId == contractId)
                .OrderByDescending(wt => wt.TransactionDate)
                .ToListAsync();
        }

        public async Task<WalletTransaction> AddAsync(WalletTransaction transaction)
        {
            _context.WalletTransactions.Add(transaction);
            await _context.SaveChangesAsync();
            return transaction;
        }

        public async Task<WalletTransaction> UpdateAsync(WalletTransaction transaction)
        {
            _context.WalletTransactions.Update(transaction);
            await _context.SaveChangesAsync();
            return transaction;
        }

        public async Task<WalletTransaction?> GetByIdAsync(Guid transactionId)
        {
            return await _context.WalletTransactions
                .Include(wt => wt.Parent)
                .FirstOrDefaultAsync(wt => wt.TransactionId == transactionId);
        }

        public async Task<bool> ExistsAsync(Guid transactionId)
        {
            return await _context.WalletTransactions
                .AnyAsync(wt => wt.TransactionId == transactionId);
        }

        public async Task<List<WalletTransaction>> GetAllAsync()
        {
            return await _context.WalletTransactions
                .Include(wt => wt.Parent)
                .OrderByDescending(wt => wt.TransactionDate)
                .ToListAsync();
        }
    }
}