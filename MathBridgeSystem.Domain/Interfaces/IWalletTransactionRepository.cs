using MathBridge.Domain.Entities;

namespace MathBridge.Domain.Interfaces
{
    public interface IWalletTransactionRepository
    {
        Task<List<WalletTransaction>> GetByParentIdAsync(Guid parentId);
        Task<WalletTransaction> AddAsync(WalletTransaction transaction);
        Task<WalletTransaction> UpdateAsync(WalletTransaction transaction);
        Task<WalletTransaction?> GetByIdAsync(Guid transactionId);
        Task<bool> ExistsAsync(Guid transactionId);
    }
}