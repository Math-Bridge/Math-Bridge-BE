using MathBridgeSystem.Application.DTOs.Review;\using MathBridgeSystem.Application.Interfaces;
using MathBridgeSystem.Domain.Entities;
using MathBridgeSystem.Domain.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MathBridgeSystem.Application.Services
{
    public class ReviewService : IReviewService
    {
        private readonly IReviewRepository _reviewRepository;
        private readonly IUserRepository _userRepository;

        public ReviewService(IReviewRepository reviewRepository, IUserRepository userRepository)
        {
            _reviewRepository = reviewRepository ?? throw new ArgumentNullException(nameof(reviewRepository));
            _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
        }

        public async Task<ReviewDto> GetReviewByIdAsync(Guid reviewId)
        {
            var review = await _reviewRepository.GetByIdAsync(reviewId);
            if (review == null)
                throw new KeyNotFoundException($"Review with ID {reviewId} not found.");

            return MapToDto(review);
        }

        public async Task<IEnumerable<ReviewDto>> GetReviewsByUserIdAsync(Guid userId)
        {
            var reviews = await _reviewRepository.GetByReviewedUserIdAsync(userId);
            return reviews.Select(MapToDto);
        }

        public async Task<IEnumerable<ReviewDto>> GetReviewsByReviewerIdAsync(Guid reviewerId)
        {
            var reviews = await _reviewRepository.GetByReviewerIdAsync(reviewerId);
            return reviews.Select(MapToDto);
        }

        public async Task<Guid> CreateReviewAsync(CreateReviewRequest request, Guid reviewerId)
        {
            // Validate user exists
            var user = await _userRepository.GetByIdAsync(request.UserId);
            if (user == null)
                throw new ArgumentException($"User with ID {request.UserId} not found.");

            // Check reviewer exists
            var reviewer = await _userRepository.GetByIdAsync(reviewerId);
            if (reviewer == null)
                throw new ArgumentException($"Reviewer with ID {reviewerId} not found.");

            var review = new Review
            {
                ReviewId = Guid.NewGuid(),
                UserId = request.UserId,
                Rating = request.Rating,
                ReviewTitle = request.ReviewTitle,
                ReviewText = request.ReviewText,
                ReviewStatus = "Active",
                CreatedDate = DateTime.UtcNow
            };

            var createdReview = await _reviewRepository.AddAsync(review);
            return createdReview.ReviewId;
        }

        public async Task UpdateReviewAsync(Guid reviewId, UpdateReviewRequest request, Guid userId)
        {
            var review = await _reviewRepository.GetByIdAsync(reviewId);
            if (review == null)
                throw new KeyNotFoundException($"Review with ID {reviewId} not found.");

            // Only allow the review owner to update
            if (review.UserId != userId)
                throw new UnauthorizedAccessException("You can only update your own reviews.");

            if (request.Rating.HasValue)
                review.Rating = request.Rating.Value;

            if (!string.IsNullOrEmpty(request.ReviewTitle))
                review.ReviewTitle = request.ReviewTitle;

            if (!string.IsNullOrEmpty(request.ReviewText))
                review.ReviewText = request.ReviewText;

            if (!string.IsNullOrEmpty(request.ReviewStatus))
                review.ReviewStatus = request.ReviewStatus;

            await _reviewRepository.UpdateAsync(review);
        }

        public async Task<bool> DeleteReviewAsync(Guid reviewId, Guid userId)
        {
            var review = await _reviewRepository.GetByIdAsync(reviewId);
            if (review == null)
                throw new KeyNotFoundException($"Review with ID {reviewId} not found.");

            // Only allow the review owner to delete
            if (review.UserId != userId)
                throw new UnauthorizedAccessException("You can only delete your own reviews.");

            return await _reviewRepository.DeleteAsync(reviewId);
        }

        public async Task<double> GetAverageRatingForUserAsync(Guid userId)
        {
            var reviews = await _reviewRepository.GetByReviewedUserIdAsync(userId);
            var activeReviews = reviews.Where(r => r.ReviewStatus == "Active").ToList();

            if (!activeReviews.Any())
                return 0;

            return activeReviews.Average(r => r.Rating);
        }

        private ReviewDto MapToDto(Review review)
        {
            return new ReviewDto
            {
                ReviewId = review.ReviewId,
                UserId = review.UserId,
                UserName = review.User?.FullName ?? "Unknown",
                Rating = review.Rating,
                ReviewTitle = review.ReviewTitle,
                ReviewText = review.ReviewText,
                ReviewStatus = review.ReviewStatus,
                CreatedDate = review.CreatedDate
            };
        }
    }
}