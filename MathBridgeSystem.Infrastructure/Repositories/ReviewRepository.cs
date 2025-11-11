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
    public class ReviewRepository : IReviewRepository
    {
        private readonly MathBridgeDbContext _context;

        public ReviewRepository(MathBridgeDbContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        public async Task<Review> GetByIdAsync(Guid id)
        {
            return await _context.Reviews
                .Include(r => r.User)
                .FirstOrDefaultAsync(r => r.ReviewId == id);
        }

        public async Task<IEnumerable<Review>> GetByReviewedUserIdAsync(Guid reviewedUserId)
        {
            return await _context.Reviews
                .Include(r => r.User)
                .Where(r => r.UserId == reviewedUserId)
                .ToListAsync();
        }

        public async Task<IEnumerable<Review>> GetByReviewerIdAsync(Guid reviewerId)
        {
            return await _context.Reviews
                .Include(r => r.User)
                .Where(r => r.UserId == reviewerId)
                .ToListAsync();
        }

        public async Task<Review> AddAsync(Review review)
        {
            _context.Reviews.Add(review);
            await _context.SaveChangesAsync();
            return review;
        }

        public async Task<Review> UpdateAsync(Review review)
        {
            _context.Reviews.Update(review);
            await _context.SaveChangesAsync();
            return review;
        }

        public async Task<bool> DeleteAsync(Guid id)
        {
            var review = await _context.Reviews.FirstOrDefaultAsync(r => r.ReviewId == id);
            if (review != null)
            {
                _context.Reviews.Remove(review);
                await _context.SaveChangesAsync();
                return true;
            }
            return false;
        }
        public async Task<List<Review>> GetAllAsync()
        {
            return await _context.Reviews
                .Include(r => r.User)
                .ToListAsync();
        }
    }
}
