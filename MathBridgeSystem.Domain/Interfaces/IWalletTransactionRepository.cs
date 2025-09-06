using MathBridge.Domain.Entities;

namespace MathBridge.Domain.Interfaces
{
    public interface IWalletTransactionRepository
    {
        Task<List<WalletTransaction>> GetByParentIdAsync(Guid parentId);
    }
}