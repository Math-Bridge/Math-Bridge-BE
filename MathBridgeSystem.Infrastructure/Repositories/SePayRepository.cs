using MathBridge.Application.Interfaces;
using MathBridge.Domain.Entities;
using MathBridge.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace MathBridge.Infrastructure.Repositories;

/// <summary>
/// Repository implementation for SePay transaction data access
/// </summary>
public class SePayRepository : ISePayRepository
{
    private readonly MathBridgeDbContext _context;

    public SePayRepository(MathBridgeDbContext context)
    {
        _context = context;
    }

    public async Task<SePayTransaction> AddAsync(SePayTransaction transaction)
    {
        _context.SePayTransactions.Add(transaction);
        await _context.SaveChangesAsync();
        return transaction;
    }

    public async Task<SePayTransaction?> GetByIdAsync(Guid id)
    {
        return await _context.SePayTransactions
            .Include(s => s.WalletTransaction)
                .ThenInclude(w => w.Parent)
            .FirstOrDefaultAsync(s => s.SePayTransactionId == id);
    }

    public async Task<SePayTransaction?> GetByWalletTransactionIdAsync(Guid walletTransactionId)
    {
        return await _context.SePayTransactions
            .Include(s => s.WalletTransaction)
                .ThenInclude(w => w.Parent)
            .FirstOrDefaultAsync(s => s.WalletTransactionId == walletTransactionId);
    }

    public async Task<SePayTransaction?> GetByOrderReferenceAsync(string orderReference)
    {
        return await _context.SePayTransactions
            .Include(s => s.WalletTransaction)
                .ThenInclude(w => w.Parent)
            .FirstOrDefaultAsync(s => s.OrderReference == orderReference);
    }

    public async Task<SePayTransaction?> ExistsByCodeAsync(string code)
    {
        return await _context.SePayTransactions.Include(s => s.WalletTransaction)
            .FirstOrDefaultAsync(s => s.Code == code);
    }

    public async Task<IEnumerable<SePayTransaction>> GetByUserIdAsync(Guid userId, int pageNumber = 1, int pageSize = 10)
    {
        var skip = (pageNumber - 1) * pageSize;
        
        return await _context.SePayTransactions
            .Include(s => s.WalletTransaction)
            .Where(s => s.WalletTransaction.ParentId == userId)
            .OrderByDescending(s => s.CreatedAt)
            .Skip(skip)
            .Take(pageSize)
            .ToListAsync();
    }

    public async Task<SePayTransaction> UpdateAsync(SePayTransaction transaction)
    {
        _context.SePayTransactions.Update(transaction);
        await _context.SaveChangesAsync();
        return transaction;
    }

    public async Task<IEnumerable<SePayTransaction>> GetPendingTransactionsAsync()
    {
        return await _context.SePayTransactions
            .Include(s => s.WalletTransaction)
            .Where(s => s.WalletTransaction.Status == "Pending")
            .OrderBy(s => s.CreatedAt)
            .ToListAsync();
    }

    public async Task<IEnumerable<SePayTransaction>> GetByDateRangeAsync(DateTime startDate, DateTime endDate)
    {
        return await _context.SePayTransactions
            .Include(s => s.WalletTransaction)
            .Where(s => s.TransactionDate >= startDate && s.TransactionDate <= endDate)
            .OrderByDescending(s => s.TransactionDate)
            .ToListAsync();
    }
}