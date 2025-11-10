using MathBridgeSystem.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MathBridgeSystem.Domain.Interfaces
{
    public interface IFinalFeedbackRepository
    {
        Task<FinalFeedback?> GetByIdAsync(Guid feedbackId);
        Task<List<FinalFeedback>> GetAllAsync();
        Task<List<FinalFeedback>> GetByUserIdAsync(Guid userId);
        Task<List<FinalFeedback>> GetByContractIdAsync(Guid contractId);
        Task<FinalFeedback?> GetByContractAndProviderTypeAsync(Guid contractId, string providerType);
        Task<List<FinalFeedback>> GetByProviderTypeAsync(string providerType);
        Task<List<FinalFeedback>> GetByStatusAsync(string status);
        Task AddAsync(FinalFeedback feedback);
        Task UpdateAsync(FinalFeedback feedback);
        Task DeleteAsync(Guid feedbackId);
    }
}