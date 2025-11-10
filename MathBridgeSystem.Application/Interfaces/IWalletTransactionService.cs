using MathBridgeSystem.Application.DTOs.WalletTransaction;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MathBridgeSystem.Application.Interfaces
{
    public interface IWalletTransactionService
    {
        Task<WalletTransactionDto> GetTransactionByIdAsync(Guid transactionId);
        Task<IEnumerable<WalletTransactionDto>> GetTransactionsByParentIdAsync(Guid parentId);
        Task<Guid> CreateTransactionAsync(CreateWalletTransactionRequest request);
        Task UpdateTransactionStatusAsync(Guid transactionId, string status);
        Task<decimal> GetParentWalletBalanceAsync(Guid parentId);
    }
}