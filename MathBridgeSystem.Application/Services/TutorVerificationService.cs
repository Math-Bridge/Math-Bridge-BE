using MathBridgeSystem.Application.DTOs.TutorVerification;
using MathBridgeSystem.Application.Interfaces;
using MathBridgeSystem.Domain.Entities;
using MathBridgeSystem.Domain.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MathBridgeSystem.Application.Services
{
    public class TutorVerificationService : ITutorVerificationService
    {
        private readonly ITutorVerificationRepository _repository;
        private readonly IUserRepository _userRepository;

        public TutorVerificationService(
            ITutorVerificationRepository repository,
            IUserRepository userRepository)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
            _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
        }

        // CRUD Operations
        public async Task<Guid> CreateVerificationAsync(CreateTutorVerificationRequest request)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            if (request.UserId == Guid.Empty)
                throw new ArgumentException("User ID is required", nameof(request.UserId));

            if (string.IsNullOrWhiteSpace(request.University))
                throw new ArgumentException("University is required", nameof(request.University));

            if (string.IsNullOrWhiteSpace(request.Major))
                throw new ArgumentException("Major is required", nameof(request.Major));

            if (request.HourlyRate <= 0)
                throw new ArgumentException("Hourly rate must be greater than 0", nameof(request.HourlyRate));
            
            // Verify user exists
            var user = await _userRepository.GetByIdAsync(request.UserId);
            if (user == null)
                throw new KeyNotFoundException($"User with ID {request.UserId} not found.");
            if (user.Role == null || user.Role.RoleName != "tutor")
                throw new InvalidOperationException($"User with ID {request.UserId} is not a tutor.");

            // Check if verification already exists
            var existingVerification = await _repository.GetByUserIdAsync(request.UserId);
            if (existingVerification != null)
                throw new InvalidOperationException($"Verification already exists for user {request.UserId}.");

            var verification = new TutorVerification
            {
                VerificationId = Guid.NewGuid(),
                UserId = request.UserId,
                University = request.University,
                Major = request.Major,
                HourlyRate = request.HourlyRate,
                Bio = request.Bio,
                VerificationStatus = "pending",
                IsDeleted = false,
                CreatedDate = DateTime.UtcNow.ToLocalTime()
            };

            await _repository.AddAsync(verification);
            return verification.VerificationId;
        }

        public async Task UpdateVerificationAsync(Guid verificationId, UpdateTutorVerificationRequest request)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            if (verificationId == Guid.Empty)
                throw new ArgumentException("Verification ID is required", nameof(verificationId));

            var verification = await _repository.GetByIdAsync(verificationId);
            if (verification == null)
                throw new KeyNotFoundException($"Tutor verification with ID {verificationId} not found.");

            bool hasChanges = false;

            if (!string.IsNullOrWhiteSpace(request.University) && request.University != verification.University)
            {
                verification.University = request.University;
                hasChanges = true;
            }

            if (!string.IsNullOrWhiteSpace(request.Major) && request.Major != verification.Major)
            {
                verification.Major = request.Major;
                hasChanges = true;
            }

            if (request.HourlyRate.HasValue && request.HourlyRate.Value > 0 && request.HourlyRate != verification.HourlyRate)
            {
                verification.HourlyRate = request.HourlyRate.Value;
                hasChanges = true;
            }

            if (request.Bio != verification.Bio)
            {
                verification.Bio = request.Bio;
                hasChanges = true;
            }

            if (hasChanges)
            {
                await _repository.UpdateAsync(verification);
            }
        }

        public async Task<TutorVerificationDto?> GetVerificationByIdAsync(Guid verificationId)
        {
            var verification = await _repository.GetByIdAsync(verificationId);
            if (verification == null)
                return null;

            return MapToDto(verification);
        }

        public async Task<TutorVerificationDto?> GetVerificationByUserIdAsync(Guid userId)
        {
            if (userId == Guid.Empty)
                throw new ArgumentException("User ID is required", nameof(userId));

            var verification = await _repository.GetByUserIdAsync(userId);
            if (verification == null)
                return null;

            return MapToDto(verification);
        }

        public async Task<List<TutorVerificationDto>> GetAllVerificationsAsync()
        {
            var verifications = await _repository.GetAllAsync();
            return verifications.Select(MapToDto).ToList();
        }

        public async Task SoftDeleteVerificationAsync(Guid verificationId)
        {
            if (verificationId == Guid.Empty)
                throw new ArgumentException("Verification ID is required", nameof(verificationId));

            var verification = await _repository.GetByIdAsync(verificationId);
            if (verification == null)
                throw new KeyNotFoundException($"Tutor verification with ID {verificationId} not found.");

            await _repository.SoftDeleteAsync(verificationId);
        }

        // Status-based Queries
        public async Task<List<TutorVerificationDto>> GetPendingVerificationsAsync()
        {
            var verifications = await _repository.GetPendingAsync();
            return verifications.Select(MapToDto).ToList();
        }

        public async Task<List<TutorVerificationDto>> GetApprovedVerificationsAsync()
        {
            var verifications = await _repository.GetApprovedAsync();
            return verifications.Select(MapToDto).ToList();
        }

        public async Task<List<TutorVerificationDto>> GetRejectedVerificationsAsync()
        {
            var verifications = await _repository.GetRejectedAsync();
            return verifications.Select(MapToDto).ToList();
        }

        public async Task<List<TutorVerificationDto>> GetVerificationsByStatusAsync(string status)
        {
            if (string.IsNullOrWhiteSpace(status))
                throw new ArgumentException("Status is required", nameof(status));

            var verifications = await _repository.GetByStatusAsync(status);
            return verifications.Select(MapToDto).ToList();
        }

        // Approval Operations
        public async Task ApproveVerificationAsync(Guid verificationId)
        {
            if (verificationId == Guid.Empty)
                throw new ArgumentException("Verification ID is required", nameof(verificationId));

            var verification = await _repository.GetByIdAsync(verificationId);
            if (verification == null)
                throw new KeyNotFoundException($"Tutor verification with ID {verificationId} not found.");

            verification.VerificationStatus = "approved";
            verification.VerificationDate = DateTime.UtcNow.ToLocalTime();
            await _repository.UpdateAsync(verification);
        }

        public async Task RejectVerificationAsync(Guid verificationId)
        {
            if (verificationId == Guid.Empty)
                throw new ArgumentException("Verification ID is required", nameof(verificationId));

            var verification = await _repository.GetByIdAsync(verificationId);
            if (verification == null)
                throw new KeyNotFoundException($"Tutor verification with ID {verificationId} not found.");

            verification.VerificationStatus = "rejected";
            verification.VerificationDate = DateTime.UtcNow.ToLocalTime();
            // Note: Could extend TutorVerification entity to include RejectionReason field if needed
            await _repository.UpdateAsync(verification);
        }

        public async Task PendingVerificationAsync(Guid verificationId)
        {
            if (verificationId == Guid.Empty)
                throw new ArgumentException("Verification ID is required", nameof(verificationId));

            var verification = await _repository.GetByIdAsync(verificationId);
            if (verification == null)
                throw new KeyNotFoundException($"Tutor verification with ID {verificationId} not found.");

            verification.VerificationStatus = "pending";
            await _repository.UpdateAsync(verification);
        }

        // Deleted Records Operations
        public async Task<List<TutorVerificationDto>> GetDeletedVerificationsAsync()
        {
            var verifications = await _repository.GetDeletedAsync();
            return verifications.Select(MapToDto).ToList();
        }

        public async Task<TutorVerificationDto?> GetDeletedVerificationByIdAsync(Guid verificationId)
        {
            var verification = await _repository.GetDeletedByIdAsync(verificationId);
            if (verification == null)
                return null;

            return MapToDto(verification);
        }

        public async Task RestoreVerificationAsync(Guid verificationId)
        {
            if (verificationId == Guid.Empty)
                throw new ArgumentException("Verification ID is required", nameof(verificationId));

            var verification = await _repository.GetDeletedByIdAsync(verificationId);
            if (verification == null)
                throw new KeyNotFoundException($"Deleted tutor verification with ID {verificationId} not found.");

            await _repository.RestoreAsync(verificationId);
        }

        public async Task PermanentlyDeleteVerificationAsync(Guid verificationId)
        {
            if (verificationId == Guid.Empty)
                throw new ArgumentException("Verification ID is required", nameof(verificationId));

            var verification = await _repository.GetByIdAsync(verificationId);
            if (verification == null && await _repository.GetDeletedByIdAsync(verificationId) == null)
                throw new KeyNotFoundException($"Tutor verification with ID {verificationId} not found.");

            await _repository.PermanentDeleteAsync(verificationId);
        }

        // Existence Checks
        public async Task<bool> VerificationExistsByUserIdAsync(Guid userId)
        {
            if (userId == Guid.Empty)
                throw new ArgumentException("User ID is required", nameof(userId));

            return await _repository.ExistsByUserIdAsync(userId);
        }

        public async Task<bool> VerificationExistsAsync(Guid verificationId)
        {
            if (verificationId == Guid.Empty)
                throw new ArgumentException("Verification ID is required", nameof(verificationId));

            var verification = await _repository.GetByIdAsync(verificationId);
            return verification != null;
        }

        // Helper Methods
        private TutorVerificationDto MapToDto(TutorVerification verification)
        {
            return new TutorVerificationDto
            {
                VerificationId = verification.VerificationId,
                UserId = verification.UserId,
                UserFullName = verification.User?.FullName,
                UserEmail = verification.User?.Email,
                University = verification.University,
                Major = verification.Major,
                HourlyRate = verification.HourlyRate,
                Bio = verification.Bio,
                VerificationStatus = verification.VerificationStatus,
                VerificationDate = verification.VerificationDate,
                CreatedDate = verification.CreatedDate,
                IsDeleted = verification.IsDeleted ?? false
            };
        }
    }
}
