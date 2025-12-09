using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MathBridgeSystem.Application.DTOs.Withdrawal;
using MathBridgeSystem.Domain.Entities;

namespace MathBridgeSystem.Application.Interfaces
{
    public interface IWithdrawalService
    {
        Task<WithdrawalRequest> RequestWithdrawalAsync(Guid userId, WithdrawalRequestCreateDto requestDto);
        Task<WithdrawalRequest> ProcessWithdrawalAsync(Guid withdrawalId, Guid staffId);
        Task<IEnumerable<WithdrawalRequest>> GetPendingRequestsAsync();
        Task<IEnumerable<WithdrawalRequest>> GetMyRequestsAsync(Guid userId);
    }
}