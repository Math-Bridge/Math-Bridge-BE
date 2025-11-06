using MathBridgeSystem.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MathBridgeSystem.Domain.Interfaces
{
    public interface ITutorVerificationRepository
    {
        // Basic CRUD Methods
        Task AddAsync(TutorVerification verification);
        Task UpdateAsync(TutorVerification verification);
        Task<TutorVerification?> GetByIdAsync(Guid verificationId);
        Task<List<TutorVerification>> GetAllAsync();
        Task SoftDeleteAsync(Guid verificationId);

        // Query Methods
        Task<TutorVerification?> GetByUserIdAsync(Guid userId);
        Task<List<TutorVerification>> GetByStatusAsync(string verificationStatus);
        Task<List<TutorVerification>> GetPendingAsync();
        Task<List<TutorVerification>> GetApprovedAsync();
        Task<List<TutorVerification>> GetRejectedAsync();
        Task<bool> ExistsByUserIdAsync(Guid userId);

        // Deleted Records Methods
        Task<List<TutorVerification>> GetDeletedAsync();
        Task<TutorVerification?> GetDeletedByIdAsync(Guid verificationId);
        Task PermanentDeleteAsync(Guid verificationId);
        Task RestoreAsync(Guid verificationId);
    }
}
