using MathBridgeSystem.Application.DTOs.FinalFeedback;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MathBridgeSystem.Application.Interfaces
{
    public interface IFinalFeedbackService
    {
        Task<FinalFeedbackDto?> GetByIdAsync(Guid feedbackId);
        Task<List<FinalFeedbackDto>> GetAllAsync();
        Task<List<FinalFeedbackDto>> GetByUserIdAsync(Guid userId);
        Task<List<FinalFeedbackDto>> GetByContractIdAsync(Guid contractId);
        Task<FinalFeedbackDto?> GetByContractAndProviderTypeAsync(Guid contractId, string providerType);
        Task<List<FinalFeedbackDto>> GetByProviderTypeAsync(string providerType);
        Task<List<FinalFeedbackDto>> GetByStatusAsync(string status);
        Task<FinalFeedbackDto> CreateAsync(CreateFinalFeedbackRequest request, Guid parentId);
        Task<FinalFeedbackDto?> UpdateAsync(Guid feedbackId, UpdateFinalFeedbackRequest request);
        Task<bool> DeleteAsync(Guid feedbackId);
    }
}