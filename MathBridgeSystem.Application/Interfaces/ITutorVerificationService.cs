using MathBridgeSystem.Application.DTOs.TutorVerification;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MathBridgeSystem.Application.Interfaces
{
    public interface ITutorVerificationService
    {
        // CRUD Operations
        Task<Guid> CreateVerificationAsync(CreateTutorVerificationRequest request);
        Task UpdateVerificationAsync(Guid verificationId, UpdateTutorVerificationRequest request);
        Task<TutorVerificationDto?> GetVerificationByIdAsync(Guid verificationId);
        Task<TutorVerificationDto?> GetVerificationByUserIdAsync(Guid userId);
        Task<List<TutorVerificationDto>> GetAllVerificationsAsync();
        Task SoftDeleteVerificationAsync(Guid verificationId);

        // Status-based Queries
        Task<List<TutorVerificationDto>> GetPendingVerificationsAsync();
        Task<List<TutorVerificationDto>> GetApprovedVerificationsAsync();
        Task<List<TutorVerificationDto>> GetRejectedVerificationsAsync();
        Task<List<TutorVerificationDto>> GetVerificationsByStatusAsync(string status);

        // Approval Operations
        Task ApproveVerificationAsync(Guid verificationId);
        Task RejectVerificationAsync(Guid verificationId);
        Task PendingVerificationAsync(Guid verificationId);

        // Deleted Records Operations
        Task<List<TutorVerificationDto>> GetDeletedVerificationsAsync();
        Task<TutorVerificationDto?> GetDeletedVerificationByIdAsync(Guid verificationId);
        Task RestoreVerificationAsync(Guid verificationId);
        Task PermanentlyDeleteVerificationAsync(Guid verificationId);

        // Existence Checks
        Task<bool> VerificationExistsByUserIdAsync(Guid userId);
        Task<bool> VerificationExistsAsync(Guid verificationId);
    }
}
