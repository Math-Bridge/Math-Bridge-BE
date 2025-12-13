using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MathBridgeSystem.Application.DTOs.Withdrawal;

namespace MathBridgeSystem.Application.Interfaces
{
    public interface IWithdrawalService
    {
        Task<WithdrawalResponseDTO> RequestWithdrawalAsync(Guid userId, WithdrawalRequestCreateDto requestDto);
        Task<WithdrawalResponseDTO> ProcessWithdrawalAsync(Guid withdrawalId, Guid staffId);
        Task<IEnumerable<WithdrawalResponseDTO>> GetPendingRequestsAsync();
        Task<IEnumerable<WithdrawalResponseDTO>> GetMyRequestsAsync(Guid userId);
    }
}