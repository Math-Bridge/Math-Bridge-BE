using MathBridgeSystem.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MathBridgeSystem.Domain.Interfaces
{
    public interface IWithdrawalRequestRepository
    {
        Task<List<WithdrawalRequest>> GetAllAsync();
        Task<WithdrawalRequest?> GetByIdAsync(Guid id);
        Task<WithdrawalRequest> AddAsync(WithdrawalRequest request);
        Task<WithdrawalRequest> UpdateAsync(WithdrawalRequest request);
        Task<List<WithdrawalRequest>> GetByParentIdAsync(Guid parentId);
    }
}
