using FluentAssertions;
using MathBridgeSystem.Application.DTOs.FinalFeedback;
using MathBridgeSystem.Application.Services;
using MathBridgeSystem.Domain.Entities;
using MathBridgeSystem.Domain.Interfaces;
using Moq;
using Xunit;

namespace MathBridgeSystem.Tests.Services
{
    public class FinalFeedbackServiceComprehensiveTests
    {
        private readonly Mock<IFinalFeedbackRepository> _repo;
        private readonly FinalFeedbackService _service;

        public FinalFeedbackServiceComprehensiveTests()
        {
            _repo = new Mock<IFinalFeedbackRepository>();
            _service = new FinalFeedbackService(_repo.Object);
        }

        [Fact]
        public void Ctor_NullRepo_Throws()
        {
            Action act = () => new FinalFeedbackService(null!);
            act.Should().Throw<ArgumentNullException>();
        }

        [Fact]
        public async Task GetByIdAsync_Found_MapsDto()
        {
            var id = Guid.NewGuid();
            var entity = new FinalFeedback
            {
                FeedbackId = id,
                UserId = Guid.NewGuid(),
                ContractId = Guid.NewGuid(),
                FeedbackProviderType = "parent",
                FeedbackText = "Great",
                OverallSatisfactionRating = 5,
                CommunicationRating = 4,
                SessionQualityRating = 5,
                LearningProgressRating = 4,
                ProfessionalismRating = 5,
                WouldRecommend = true,
                WouldWorkTogetherAgain = true,
                ContractObjectivesMet = true,
                ImprovementSuggestions = "None",
                AdditionalComments = "Nice",
                FeedbackStatus = "Submitted",
                CreatedDate = DateTime.UtcNow
            };
            _repo.Setup(r => r.GetByIdAsync(id)).ReturnsAsync(entity);

            var dto = await _service.GetByIdAsync(id);

            dto.Should().NotBeNull();
            dto!.FeedbackId.Should().Be(id);
            dto.FeedbackProviderType.Should().Be("parent");
        }

        [Fact]
        public async Task GetByIdAsync_NotFound_ReturnsNull()
        {
            _repo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((FinalFeedback)null!);
            var dto = await _service.GetByIdAsync(Guid.NewGuid());
            dto.Should().BeNull();
        }

        [Fact]
        public async Task GetAllAsync_MapsList()
        {
            _repo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<FinalFeedback>
            {
                new FinalFeedback { FeedbackId = Guid.NewGuid(), OverallSatisfactionRating = 5 },
                new FinalFeedback { FeedbackId = Guid.NewGuid(), OverallSatisfactionRating = 4 }
            });

            var result = await _service.GetAllAsync();
            result.Should().HaveCount(2);
            result.Select(r => r.OverallSatisfactionRating).Should().Contain(new[] { 5, 4 });
        }

        [Fact]
        public async Task GetByUserIdAsync_ReturnsDtos()
        {
            var userId = Guid.NewGuid();
            _repo.Setup(r => r.GetByUserIdAsync(userId)).ReturnsAsync(new List<FinalFeedback>
            {
                new FinalFeedback { FeedbackId = Guid.NewGuid(), UserId = userId }
            });

            var result = await _service.GetByUserIdAsync(userId);
            result.Should().HaveCount(1);
            result[0].UserId.Should().Be(userId);
        }

        [Fact]
        public async Task GetByContractIdAsync_ReturnsDtos()
        {
            var contractId = Guid.NewGuid();
            _repo.Setup(r => r.GetByContractIdAsync(contractId)).ReturnsAsync(new List<FinalFeedback>
            {
                new FinalFeedback { FeedbackId = Guid.NewGuid(), ContractId = contractId }
            });

            var result = await _service.GetByContractIdAsync(contractId);
            result.Should().HaveCount(1);
            result[0].ContractId.Should().Be(contractId);
        }

        [Fact]
        public async Task GetByContractAndProviderTypeAsync_Found_ReturnsDto()
        {
            var contractId = Guid.NewGuid();
            _repo.Setup(r => r.GetByContractAndProviderTypeAsync(contractId, "parent"))
                .ReturnsAsync(new FinalFeedback { FeedbackId = Guid.NewGuid(), ContractId = contractId, FeedbackProviderType = "parent" });

            var dto = await _service.GetByContractAndProviderTypeAsync(contractId, "parent");
            dto.Should().NotBeNull();
            dto!.FeedbackProviderType.Should().Be("parent");
        }

        [Fact]
        public async Task GetByProviderTypeAsync_ReturnsList()
        {
            _repo.Setup(r => r.GetByProviderTypeAsync("tutor"))
                .ReturnsAsync(new List<FinalFeedback> { new FinalFeedback { FeedbackProviderType = "tutor" } });

            var list = await _service.GetByProviderTypeAsync("tutor");
            list.Should().HaveCount(1);
        }

        [Fact]
        public async Task GetByStatusAsync_ReturnsList()
        {
            _repo.Setup(r => r.GetByStatusAsync("Submitted"))
                .ReturnsAsync(new List<FinalFeedback> { new FinalFeedback { FeedbackStatus = "Submitted" } });

            var list = await _service.GetByStatusAsync("Submitted");
            list.Should().HaveCount(1);
        }

        [Fact]
        public async Task CreateAsync_MapsAndAdds()
        {
            var req = new CreateFinalFeedbackRequest
            {
                UserId = Guid.NewGuid(),
                ContractId = Guid.NewGuid(),
                FeedbackProviderType = "parent",
                FeedbackText = "Good",
                OverallSatisfactionRating = 5,
                CommunicationRating = 4,
                SessionQualityRating = 5,
                LearningProgressRating = 4,
                ProfessionalismRating = 5,
                WouldRecommend = true,
                WouldWorkTogetherAgain = true,
                ContractObjectivesMet = true,
                ImprovementSuggestions = "-",
                AdditionalComments = "-"
            };

            FinalFeedback? captured = null;
            _repo.Setup(r => r.AddAsync(It.IsAny<FinalFeedback>())).Callback<FinalFeedback>(f => captured = f).Returns(Task.CompletedTask);

            var dto = await _service.CreateAsync(req);

            dto.FeedbackProviderType.Should().Be("parent");
            captured.Should().NotBeNull();
            captured!.FeedbackStatus.Should().Be("Submitted");
        }

        [Fact]
        public async Task UpdateAsync_NotFound_ReturnsNull()
        {
            _repo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((FinalFeedback)null!);
            var dto = await _service.UpdateAsync(Guid.NewGuid(), new UpdateFinalFeedbackRequest());
            dto.Should().BeNull();
        }

        [Fact]
        public async Task UpdateAsync_UpdatesProvidedFields()
        {
            var id = Guid.NewGuid();
            var entity = new FinalFeedback
            {
                FeedbackId = id,
                FeedbackText = "old",
                OverallSatisfactionRating = 3,
                CommunicationRating = 3,
                SessionQualityRating = 3,
                LearningProgressRating = 3,
                ProfessionalismRating = 3,
                WouldRecommend = false,
                WouldWorkTogetherAgain = false,
                ContractObjectivesMet = false,
                ImprovementSuggestions = null,
                AdditionalComments = null,
                FeedbackStatus = "Submitted"
            };
            _repo.Setup(r => r.GetByIdAsync(id)).ReturnsAsync(entity);
            _repo.Setup(r => r.UpdateAsync(entity)).Returns(Task.CompletedTask);

            var req = new UpdateFinalFeedbackRequest
            {
                FeedbackText = "new",
                OverallSatisfactionRating = 5,
                CommunicationRating = 4,
                SessionQualityRating = 5,
                LearningProgressRating = 4,
                ProfessionalismRating = 5,
                WouldRecommend = true,
                WouldWorkTogetherAgain = true,
                ContractObjectivesMet = true,
                ImprovementSuggestions = "none",
                AdditionalComments = "ok",
                FeedbackStatus = "Reviewed"
            };

            var dto = await _service.UpdateAsync(id, req);

            dto.Should().NotBeNull();
            entity.FeedbackText.Should().Be("new");
            entity.OverallSatisfactionRating.Should().Be(5);
            entity.FeedbackStatus.Should().Be("Reviewed");
        }

        [Fact]
        public async Task DeleteAsync_NotFound_ReturnsFalse()
        {
            _repo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((FinalFeedback)null!);
            var ok = await _service.DeleteAsync(Guid.NewGuid());
            ok.Should().BeFalse();
        }

        [Fact]
        public async Task DeleteAsync_Found_DeletesAndReturnsTrue()
        {
            var id = Guid.NewGuid();
            _repo.Setup(r => r.GetByIdAsync(id)).ReturnsAsync(new FinalFeedback { FeedbackId = id });
            _repo.Setup(r => r.DeleteAsync(id)).Returns(Task.CompletedTask);

            var ok = await _service.DeleteAsync(id);
            ok.Should().BeTrue();
            _repo.Verify(r => r.DeleteAsync(id), Times.Once);
        }
    }
}
