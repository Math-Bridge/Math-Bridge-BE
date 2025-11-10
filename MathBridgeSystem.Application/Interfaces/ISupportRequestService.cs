using MathBridgeSystem.Application.DTOs;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MathBridgeSystem.Application.Interfaces
{
    public interface ISupportRequestService
    {
        Task<Guid> CreateSupportRequestAsync(CreateSupportRequestRequest request, Guid userId);
        Task UpdateSupportRequestAsync(Guid id, UpdateSupportRequestRequest request, Guid userId);
        Task DeleteSupportRequestAsync(Guid id, Guid userId);
        Task<SupportRequestDto?> GetSupportRequestByIdAsync(Guid id);
        Task<List<SupportRequestDto>> GetAllSupportRequestsAsync();
        Task<List<SupportRequestDto>> GetSupportRequestsByUserIdAsync(Guid userId);
        Task<List<SupportRequestDto>> GetSupportRequestsByStatusAsync(string status);
        Task<List<SupportRequestDto>> GetSupportRequestsByCategoryAsync(string category);
        Task<List<SupportRequestDto>> GetSupportRequestsByAssignedUserIdAsync(Guid assignedUserId);
        Task AssignSupportRequestAsync(Guid id, AssignSupportRequestRequest request);
        Task UpdateSupportRequestStatusAsync(Guid id, UpdateSupportRequestStatusRequest request);
    }
}