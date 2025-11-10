using MathBridgeSystem.Domain.Entities;
using MathBridgeSystem.Domain.Interfaces;
using MathBridgeSystem.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MathBridgeSystem.Infrastructure.Repositories
{
    public class FinalFeedbackRepository : IFinalFeedbackRepository
    {
        private readonly MathBridgeDbContext _context;

        public FinalFeedbackRepository(MathBridgeDbContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        public async Task<FinalFeedback?> GetByIdAsync(Guid feedbackId)
        {
            return await _context.FinalFeedbacks
                .Include(f => f.User)
                .Include(f => f.Contract)
                .FirstOrDefaultAsync(f => f.FeedbackId == feedbackId);
        }

        public async Task<List<FinalFeedback>> GetAllAsync()
        {
            return await _context.FinalFeedbacks
                .Include(f => f.User)
                .Include(f => f.Contract)
                .OrderByDescending(f => f.CreatedDate)
                .ToListAsync();
        }

        public async Task<List<FinalFeedback>> GetByUserIdAsync(Guid userId)
        {
            return await _context.FinalFeedbacks
                .Include(f => f.User)
                .Include(f => f.Contract)
                .Where(f => f.UserId == userId)
                .OrderByDescending(f => f.CreatedDate)
                .ToListAsync();
        }

        public async Task<List<FinalFeedback>> GetByContractIdAsync(Guid contractId)
        {
            return await _context.FinalFeedbacks
                .Include(f => f.User)
                .Include(f => f.Contract)
                .Where(f => f.ContractId == contractId)
                .OrderByDescending(f => f.CreatedDate)
                .ToListAsync();
        }

        public async Task<FinalFeedback?> GetByContractAndProviderTypeAsync(Guid contractId, string providerType)
        {
            return await _context.FinalFeedbacks
                .Include(f => f.User)
                .Include(f => f.Contract)
                .FirstOrDefaultAsync(f => f.ContractId == contractId && f.FeedbackProviderType == providerType);
        }

        public async Task<List<FinalFeedback>> GetByProviderTypeAsync(string providerType)
        {
            return await _context.FinalFeedbacks
                .Include(f => f.User)
                .Include(f => f.Contract)
                .Where(f => f.FeedbackProviderType == providerType)
                .OrderByDescending(f => f.CreatedDate)
                .ToListAsync();
        }

        public async Task<List<FinalFeedback>> GetByStatusAsync(string status)
        {
            return await _context.FinalFeedbacks
                .Include(f => f.User)
                .Include(f => f.Contract)
                .Where(f => f.FeedbackStatus == status)
                .OrderByDescending(f => f.CreatedDate)
                .ToListAsync();
        }

        public async Task AddAsync(FinalFeedback feedback)
        {
            _context.FinalFeedbacks.Add(feedback);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(FinalFeedback feedback)
        {
            _context.FinalFeedbacks.Update(feedback);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(Guid feedbackId)
        {
            var feedback = await _context.FinalFeedbacks.FindAsync(feedbackId);
            if (feedback != null)
            {
                _context.FinalFeedbacks.Remove(feedback);
                await _context.SaveChangesAsync();
            }
        }
    }
}