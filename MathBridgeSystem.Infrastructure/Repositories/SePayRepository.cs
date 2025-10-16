using MathBridgeSystem.Application.Interfaces;
using MathBridgeSystem.Domain.Entities;
using MathBridgeSystem.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace MathBridgeSystem.Infrastructure.Repositories;

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

    public async Task<SepayTransaction> AddAsync(SepayTransaction transaction)
    {
        _context.SepayTransactions.Add(transaction);
        await _context.SaveChangesAsync();
        return transaction;
    }

    public async Task<SepayTransaction?> GetByIdAsync(Guid id)
    {
        return await _context.SepayTransactions
            .Include(s => s.WalletTransaction)
                .ThenInclude(w => w.Parent)
            .FirstOrDefaultAsync(s => s.SepayTransactionId  == id);
    }

    public async Task<SepayTransaction?> GetByWalletTransactionIdAsync(Guid walletTransactionId)
    {
        return await _context.SepayTransactions
            .Include(s => s.WalletTransaction)
                .ThenInclude(w => w.Parent)
            .FirstOrDefaultAsync(s => s.WalletTransactionId == walletTransactionId);
    }

    public async Task<SepayTransaction?> GetByOrderReferenceAsync(string orderReference)
    {
        return await _context.SepayTransactions
            .Include(s => s.WalletTransaction)
                .ThenInclude(w => w.Parent)
            .FirstOrDefaultAsync(s => s.OrderReference == orderReference);
    }

    public async Task<SepayTransaction?> ExistsByCodeAsync(string code)
    {
        return await _context.SepayTransactions.Include(s => s.WalletTransaction)
            .FirstOrDefaultAsync(s => s.Code == code);
    }

    public async Task<IEnumerable<SepayTransaction>> GetByUserIdAsync(Guid userId, int pageNumber = 1, int pageSize = 10)
    {
        var skip = (pageNumber - 1) * pageSize;
        
        return await _context.SepayTransactions
            .Include(s => s.WalletTransaction)
            .Where(s => s.WalletTransaction.ParentId == userId)
            .OrderByDescending(s => s.CreatedAt)
            .Skip(skip)
            .Take(pageSize)
            .ToListAsync();
    }

    public async Task<SepayTransaction> UpdateAsync(SepayTransaction transaction)
    {
        _context.SepayTransactions.Update(transaction);
        await _context.SaveChangesAsync();
        return transaction;
    }

    public async Task<IEnumerable<SepayTransaction>> GetPendingTransactionsAsync()
    {
        return await _context.SepayTransactions
            .Include(s => s.WalletTransaction)
            .Where(s => s.WalletTransaction.Status == "Pending")
            .OrderBy(s => s.CreatedAt)
            .ToListAsync();
    }

    public async Task<IEnumerable<SepayTransaction>> GetByDateRangeAsync(DateTime startDate, DateTime endDate)
    {
        return await _context.SepayTransactions
            .Include(s => s.WalletTransaction)
            .Where(s => s.TransactionDate >= startDate && s.TransactionDate <= endDate)
            .OrderByDescending(s => s.TransactionDate)
            .ToListAsync();
    }
}