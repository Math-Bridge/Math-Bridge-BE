using MathBridgeSystem.Application.DTOs.FinalFeedback;
using MathBridgeSystem.Application.Interfaces;
using MathBridgeSystem.Domain.Entities;
using MathBridgeSystem.Domain.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MathBridgeSystem.Application.Services
{
    public class FinalFeedbackService : IFinalFeedbackService
    {
        private readonly IFinalFeedbackRepository _feedbackRepository;

        public FinalFeedbackService(IFinalFeedbackRepository feedbackRepository)
        {
            _feedbackRepository = feedbackRepository ?? throw new ArgumentNullException(nameof(feedbackRepository));
        }

        public async Task<FinalFeedbackDto?> GetByIdAsync(Guid feedbackId)
        {
            var feedback = await _feedbackRepository.GetByIdAsync(feedbackId);
            return feedback != null ? MapToDto(feedback) : null;
        }

        public async Task<List<FinalFeedbackDto>> GetAllAsync()
        {
            var feedbacks = await _feedbackRepository.GetAllAsync();
            return feedbacks.Select(MapToDto).ToList();
        }

        public async Task<List<FinalFeedbackDto>> GetByUserIdAsync(Guid userId)
        {
            var feedbacks = await _feedbackRepository.GetByUserIdAsync(userId);
            return feedbacks.Select(MapToDto).ToList();
        }

        public async Task<List<FinalFeedbackDto>> GetByContractIdAsync(Guid contractId)
        {
            var feedbacks = await _feedbackRepository.GetByContractIdAsync(contractId);
            return feedbacks.Select(MapToDto).ToList();
        }

        public async Task<FinalFeedbackDto?> GetByContractAndProviderTypeAsync(Guid contractId, string providerType)
        {
            var feedback = await _feedbackRepository.GetByContractAndProviderTypeAsync(contractId, providerType);
            return feedback != null ? MapToDto(feedback) : null;
        }

        public async Task<List<FinalFeedbackDto>> GetByProviderTypeAsync(string providerType)
        {
            var feedbacks = await _feedbackRepository.GetByProviderTypeAsync(providerType);
            return feedbacks.Select(MapToDto).ToList();
        }

        public async Task<List<FinalFeedbackDto>> GetByStatusAsync(string status)
        {
            var feedbacks = await _feedbackRepository.GetByStatusAsync(status);
            return feedbacks.Select(MapToDto).ToList();
        }

        public async Task<FinalFeedbackDto> CreateAsync(CreateFinalFeedbackRequest request)
        {
            var feedback = new FinalFeedback
            {
                FeedbackId = Guid.NewGuid(),
                UserId = request.UserId,
                ContractId = request.ContractId,
                FeedbackProviderType = request.FeedbackProviderType,
                FeedbackText = request.FeedbackText,
                OverallSatisfactionRating = request.OverallSatisfactionRating,
                CommunicationRating = request.CommunicationRating,
                SessionQualityRating = request.SessionQualityRating,
                LearningProgressRating = request.LearningProgressRating,
                ProfessionalismRating = request.ProfessionalismRating,
                WouldRecommend = request.WouldRecommend,
                WouldWorkTogetherAgain = request.WouldWorkTogetherAgain,
                ContractObjectivesMet = request.ContractObjectivesMet,
                ImprovementSuggestions = request.ImprovementSuggestions,
                AdditionalComments = request.AdditionalComments,
                FeedbackStatus = "Submitted",
                CreatedDate = DateTime.UtcNow
            };

            await _feedbackRepository.AddAsync(feedback);
            return MapToDto(feedback);
        }

        public async Task<FinalFeedbackDto?> UpdateAsync(Guid feedbackId, UpdateFinalFeedbackRequest request)
        {
            var feedback = await _feedbackRepository.GetByIdAsync(feedbackId);
            if (feedback == null)
            {
                return null;
            }

            if (!string.IsNullOrEmpty(request.FeedbackText))
            {
                feedback.FeedbackText = request.FeedbackText;
            }

            if (request.OverallSatisfactionRating.HasValue)
            {
                feedback.OverallSatisfactionRating = request.OverallSatisfactionRating.Value;
            }

            if (request.CommunicationRating.HasValue)
            {
                feedback.CommunicationRating = request.CommunicationRating.Value;
            }

            if (request.SessionQualityRating.HasValue)
            {
                feedback.SessionQualityRating = request.SessionQualityRating.Value;
            }

            if (request.LearningProgressRating.HasValue)
            {
                feedback.LearningProgressRating = request.LearningProgressRating.Value;
            }

            if (request.ProfessionalismRating.HasValue)
            {
                feedback.ProfessionalismRating = request.ProfessionalismRating.Value;
            }

            if (request.WouldRecommend.HasValue)
            {
                feedback.WouldRecommend = request.WouldRecommend.Value;
            }

            if (request.WouldWorkTogetherAgain.HasValue)
            {
                feedback.WouldWorkTogetherAgain = request.WouldWorkTogetherAgain.Value;
            }

            if (request.ContractObjectivesMet.HasValue)
            {
                feedback.ContractObjectivesMet = request.ContractObjectivesMet.Value;
            }

            if (!string.IsNullOrEmpty(request.ImprovementSuggestions))
            {
                feedback.ImprovementSuggestions = request.ImprovementSuggestions;
            }

            if (!string.IsNullOrEmpty(request.AdditionalComments))
            {
                feedback.AdditionalComments = request.AdditionalComments;
            }

            if (!string.IsNullOrEmpty(request.FeedbackStatus))
            {
                feedback.FeedbackStatus = request.FeedbackStatus;
            }

            await _feedbackRepository.UpdateAsync(feedback);
            return MapToDto(feedback);
        }

        public async Task<bool> DeleteAsync(Guid feedbackId)
        {
            var feedback = await _feedbackRepository.GetByIdAsync(feedbackId);
            if (feedback == null)
            {
                return false;
            }

            await _feedbackRepository.DeleteAsync(feedbackId);
            return true;
        }

        private FinalFeedbackDto MapToDto(FinalFeedback feedback)
        {
            return new FinalFeedbackDto
            {
                FeedbackId = feedback.FeedbackId,
                UserId = feedback.UserId,
                ContractId = feedback.ContractId,
                FeedbackProviderType = feedback.FeedbackProviderType,
                FeedbackText = feedback.FeedbackText,
                OverallSatisfactionRating = feedback.OverallSatisfactionRating,
                CommunicationRating = feedback.CommunicationRating,
                SessionQualityRating = feedback.SessionQualityRating,
                LearningProgressRating = feedback.LearningProgressRating,
                ProfessionalismRating = feedback.ProfessionalismRating,
                WouldRecommend = feedback.WouldRecommend,
                WouldWorkTogetherAgain = feedback.WouldWorkTogetherAgain,
                ContractObjectivesMet = feedback.ContractObjectivesMet,
                ImprovementSuggestions = feedback.ImprovementSuggestions,
                AdditionalComments = feedback.AdditionalComments,
                FeedbackStatus = feedback.FeedbackStatus,
                CreatedDate = feedback.CreatedDate,
                UserFullName = feedback.User != null ? $"{feedback.User.FirstName} {feedback.User.LastName}" : null,
                ContractTitle = feedback.Contract?.ContractTitle
            };
        }
    }
}