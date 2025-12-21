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
        return await _context.SepayTransactions
            .Include(s => s.WalletTransaction)
            .FirstOrDefaultAsync(s => s.Code == code);
    }

    public async Task<SepayTransaction?> GetByCodeAsync(string code)
    {
        return await _context.SepayTransactions
            .Include(s => s.WalletTransaction)
            .Include(s => s.Contract)
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
    public async Task<IEnumerable<SepayTransaction>> GetAllAsync()
    {
        return await _context.SepayTransactions
            .Include(s => s.WalletTransaction)
            .OrderByDescending(s => s.CreatedAt)
            .ToListAsync();
    }

    public async Task<List<SepayTransaction>> GetByContractIdAsync(Guid contractId)
    {
        return await _context.SepayTransactions
            .Include(s => s.WalletTransaction)
            .Include(s => s.Contract)
            .Where(s => s.ContractId == contractId)
            .OrderByDescending(s => s.CreatedAt)
            .ToListAsync();
    }


    public async Task<List<SepayTransaction>> GetByParentIdThroughContractsAsync(Guid parentId)
    {
        return await _context.SepayTransactions
            .Include(s => s.Contract)
                .ThenInclude(c => c.Child)
            .Include(s => s.Contract)
                .ThenInclude(c => c.Package)
            .Include(s => s.WalletTransaction)
            .Where(s => s.Contract != null && s.Contract.ParentId == parentId)
            .OrderByDescending(s => s.CreatedAt)
            .ToListAsync();
    }
}