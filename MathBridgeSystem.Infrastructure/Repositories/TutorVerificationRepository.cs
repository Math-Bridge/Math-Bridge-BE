using MathBridgeSystem.Infrastructure.Data;
using MathBridgeSystem.Domain.Entities;
using MathBridgeSystem.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MathBridgeSystem.Infrastructure.Repositories
{
    public class TutorVerificationRepository : ITutorVerificationRepository
    {
        private readonly MathBridgeDbContext _context;

        public TutorVerificationRepository(MathBridgeDbContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        // Basic CRUD Methods
        public async Task AddAsync(TutorVerification verification)
        {
            if (verification == null)
                throw new ArgumentNullException(nameof(verification));

            verification.CreatedDate = DateTime.UtcNow;
            verification.IsDeleted = false;
            _context.TutorVerifications.Add(verification);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(TutorVerification verification)
        {
            if (verification == null)
                throw new ArgumentNullException(nameof(verification));

            var existingVerification = await _context.TutorVerifications
                .FirstOrDefaultAsync(v => v.VerificationId == verification.VerificationId && v.IsDeleted == false);

            if (existingVerification == null)
                throw new KeyNotFoundException($"Tutor verification with ID {verification.VerificationId} not found.");

            existingVerification.University = verification.University;
            existingVerification.Major = verification.Major;
            existingVerification.HourlyRate = verification.HourlyRate;
            existingVerification.Bio = verification.Bio;
            existingVerification.VerificationStatus = verification.VerificationStatus;
            existingVerification.VerificationDate = verification.VerificationDate;

            _context.TutorVerifications.Update(existingVerification);
            await _context.SaveChangesAsync();
        }

        public async Task<TutorVerification?> GetByIdAsync(Guid verificationId)
        {
            return await _context.TutorVerifications
                .Include(v => v.User)
                .FirstOrDefaultAsync(v => v.VerificationId == verificationId && v.IsDeleted == false);
        }

        public async Task<List<TutorVerification>> GetAllAsync()
        {
            return await _context.TutorVerifications
                .Include(v => v.User)
                .Where(v => v.IsDeleted == false)
                .OrderBy(v => v.CreatedDate)
                .ToListAsync();
        }

        public async Task SoftDeleteAsync(Guid verificationId)
        {
            var verification = await _context.TutorVerifications
                .FirstOrDefaultAsync(v => v.VerificationId == verificationId && v.IsDeleted == false);

            if (verification == null)
                throw new KeyNotFoundException($"Tutor verification with ID {verificationId} not found.");

            verification.IsDeleted = true;
            _context.TutorVerifications.Update(verification);
            await _context.SaveChangesAsync();
        }

        // Query Methods
        public async Task<TutorVerification?> GetByUserIdAsync(Guid userId)
        {
            return await _context.TutorVerifications
                .Include(v => v.User)
                .FirstOrDefaultAsync(v => v.UserId == userId && v.IsDeleted == false);
        }

        public async Task<List<TutorVerification>> GetByStatusAsync(string verificationStatus)
        {
            if (string.IsNullOrWhiteSpace(verificationStatus))
                throw new ArgumentException("Verification status is required.", nameof(verificationStatus));

            return await _context.TutorVerifications
                .Include(v => v.User)
                .Where(v => v.VerificationStatus.ToLower() == verificationStatus.ToLower() && v.IsDeleted == false)
                .OrderBy(v => v.CreatedDate)
                .ToListAsync();
        }

        public async Task<List<TutorVerification>> GetPendingAsync()
        {
            return await GetByStatusAsync("pending");
        }

        public async Task<List<TutorVerification>> GetApprovedAsync()
        {
            return await GetByStatusAsync("approved");
        }

        public async Task<List<TutorVerification>> GetRejectedAsync()
        {
            return await GetByStatusAsync("rejected");
        }

        public async Task<bool> ExistsByUserIdAsync(Guid userId)
        {
            return await _context.TutorVerifications
                .AnyAsync(v => v.UserId == userId && v.IsDeleted == false);
        }

        // Deleted Records Methods
        public async Task<List<TutorVerification>> GetDeletedAsync()
        {
            return await _context.TutorVerifications
                .Include(v => v.User)
                .Where(v => v.IsDeleted == true)
                .OrderBy(v => v.CreatedDate)
                .ToListAsync();
        }

        public async Task<TutorVerification?> GetDeletedByIdAsync(Guid verificationId)
        {
            return await _context.TutorVerifications
                .Include(v => v.User)
                .FirstOrDefaultAsync(v => v.VerificationId == verificationId && v.IsDeleted == true);
        }

        public async Task PermanentDeleteAsync(Guid verificationId)
        {
            var verification = await _context.TutorVerifications
                .FirstOrDefaultAsync(v => v.VerificationId == verificationId);

            if (verification == null)
                throw new KeyNotFoundException($"Tutor verification with ID {verificationId} not found.");

            _context.TutorVerifications.Remove(verification);
            await _context.SaveChangesAsync();
        }

        public async Task RestoreAsync(Guid verificationId)
        {
            var verification = await _context.TutorVerifications
                .FirstOrDefaultAsync(v => v.VerificationId == verificationId && v.IsDeleted == true);

            if (verification == null)
                throw new KeyNotFoundException($"Deleted tutor verification with ID {verificationId} not found.");

            verification.IsDeleted = false;
            _context.TutorVerifications.Update(verification);
            await _context.SaveChangesAsync();
        }
    }
}
