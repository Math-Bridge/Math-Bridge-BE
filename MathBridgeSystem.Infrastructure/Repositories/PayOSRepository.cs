using MathBridge.Application.Interfaces;
using MathBridge.Domain.Entities;
using MathBridge.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace MathBridge.Infrastructure.Repositories;

/// <summary>
/// Repository implementation for PayOS transaction data access
/// </summary>
public class PayOSRepository : IPayOSRepository
{
    private readonly MathBridgeDbContext _context;

    public PayOSRepository(MathBridgeDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Create a new PayOS transaction record
    /// </summary>
    public async Task<PayOSTransaction> CreateAsync(PayOSTransaction transaction)
    {
        _context.PayOSTransactions.Add(transaction);
        await _context.SaveChangesAsync();
        return transaction;
    }

    /// <summary>
    /// Get PayOS transaction by order code
    /// </summary>
    public async Task<PayOSTransaction?> GetByOrderCodeAsync(long orderCode)
    {
        return await _context.PayOSTransactions
            .Include(p => p.WalletTransaction)
                .ThenInclude(w => w.Parent)
            .FirstOrDefaultAsync(p => p.OrderCode == orderCode);
    }

    /// <summary>
    /// Get PayOS transaction by ID
    /// </summary>
    public async Task<PayOSTransaction?> GetByIdAsync(Guid id)
    {
        return await _context.PayOSTransactions
            .Include(p => p.WalletTransaction)
                .ThenInclude(w => w.Parent)
            .FirstOrDefaultAsync(p => p.PayosTransactionId == id);
    }

    /// <summary>
    /// Get PayOS transaction by wallet transaction ID
    /// </summary>
    public async Task<PayOSTransaction?> GetByWalletTransactionIdAsync(Guid walletTransactionId)
    {
        return await _context.PayOSTransactions
            .Include(p => p.WalletTransaction)
                .ThenInclude(w => w.Parent)
            .FirstOrDefaultAsync(p => p.WalletTransactionId == walletTransactionId);
    }

    /// <summary>
    /// Update PayOS transaction
    /// </summary>
    public async Task<PayOSTransaction> UpdateAsync(PayOSTransaction transaction)
    {
        transaction.UpdatedDate = DateTime.UtcNow;
        _context.PayOSTransactions.Update(transaction);
        await _context.SaveChangesAsync();
        return transaction;
    }

    /// <summary>
    /// Get PayOS transactions by user ID through wallet transactions
    /// </summary>
    public async Task<IEnumerable<PayOSTransaction>> GetByUserIdAsync(Guid userId, int skip = 0, int take = 10)
    {
        return await _context.PayOSTransactions
            .Include(p => p.WalletTransaction)
                .ThenInclude(w => w.Parent)
            .Where(p => p.WalletTransaction.ParentId == userId)
            .OrderByDescending(p => p.CreatedDate)
            .Skip(skip)
            .Take(take)
            .ToListAsync();
    }

    /// <summary>
    /// Get pending PayOS transactions older than specified minutes
    /// </summary>
    public async Task<IEnumerable<PayOSTransaction>> GetPendingTransactionsAsync(int olderThanMinutes = 30)
    {
        var cutoffTime = DateTime.UtcNow.AddMinutes(-olderThanMinutes);
        
        return await _context.PayOSTransactions
            .Include(p => p.WalletTransaction)
                .ThenInclude(w => w.Parent)
            .Where(p => p.PaymentStatus == "PENDING" && p.CreatedDate <= cutoffTime)
            .OrderBy(p => p.CreatedDate)
            .ToListAsync();
    }
}