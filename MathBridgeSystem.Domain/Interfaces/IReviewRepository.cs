using MathBridgeSystem.Domain.Entities;

namespace MathBridgeSystem.Domain.Interfaces
{
    public interface IReviewRepository
    {
        Task<Review> GetByIdAsync(Guid id);
        Task<IEnumerable<Review>> GetByReviewedUserIdAsync(Guid reviewedUserId);
        Task<IEnumerable<Review>> GetByReviewerIdAsync(Guid reviewerId);
        Task<Review> AddAsync(Review review);
        Task<Review> UpdateAsync(Review review);
        Task<bool> DeleteAsync(Guid id);
    }
}
