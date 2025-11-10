using MathBridgeSystem.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MathBridgeSystem.Domain.Interfaces
{
    public interface ISupportRequestRepository
    {
        Task AddAsync(SupportRequest supportRequest);
        Task UpdateAsync(SupportRequest supportRequest);
        Task DeleteAsync(Guid id);
        Task<SupportRequest?> GetByIdAsync(Guid id);
        Task<List<SupportRequest>> GetAllAsync();
        Task<List<SupportRequest>> GetByUserIdAsync(Guid userId);
        Task<List<SupportRequest>> GetByStatusAsync(string status);
        Task<List<SupportRequest>> GetByCategoryAsync(string category);
        Task<List<SupportRequest>> GetByAssignedUserIdAsync(Guid assignedUserId);
        Task<bool> ExistsAsync(Guid id);
    }
}