using MathBridgeSystem.Application.DTOs.Review;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MathBridgeSystem.Application.Interfaces
{
    public interface IReviewService
    {
        Task<ReviewDto> GetReviewByIdAsync(Guid reviewId);
        Task<IEnumerable<ReviewDto>> GetReviewsByUserIdAsync(Guid userId);
        Task<IEnumerable<ReviewDto>> GetReviewsByReviewerIdAsync(Guid reviewerId);
        Task<Guid> CreateReviewAsync(CreateReviewRequest request, Guid reviewerId);
        Task UpdateReviewAsync(Guid reviewId, UpdateReviewRequest request, Guid userId);
        Task<bool> DeleteReviewAsync(Guid reviewId, Guid userId);
        Task<double> GetAverageRatingForUserAsync(Guid userId);
    }
}