using FluentAssertions;
using MathBridgeSystem.Application.DTOs.FinalFeedback;
using MathBridgeSystem.Application.Services;
using MathBridgeSystem.Domain.Entities;
using MathBridgeSystem.Domain.Interfaces;
using Moq;
using Xunit;

namespace MathBridgeSystem.Tests.Services
{
    public class FinalFeedbackServiceTests
    {
        private readonly Mock<IFinalFeedbackRepository> _feedbackRepositoryMock;
        private readonly Mock<IContractRepository> _contractRepositoryMock;
        private readonly Mock<IUserRepository> _userRepositoryMock;
        private readonly FinalFeedbackService _service;

        public FinalFeedbackServiceTests()
        {
            _feedbackRepositoryMock = new Mock<IFinalFeedbackRepository>();
            _contractRepositoryMock = new Mock<IContractRepository>();
            _userRepositoryMock = new Mock<IUserRepository>();
            _service = new FinalFeedbackService(_feedbackRepositoryMock.Object, _contractRepositoryMock.Object, _userRepositoryMock.Object);
        }

        [Fact]
        public async Task GetByIdAsync_ShouldReturnFeedback_WhenExists()
        {
            // Arrange
            var feedbackId = Guid.NewGuid();
            var feedback = new FinalFeedback
            {
                FeedbackId = feedbackId,
                UserId = Guid.NewGuid(),
                ContractId = Guid.NewGuid(),
                FeedbackProviderType = "Parent",
                OverallSatisfactionRating = 5,
                FeedbackText = "Great experience"
            };

            _feedbackRepositoryMock.Setup(r => r.GetByIdAsync(feedbackId))
                .ReturnsAsync(feedback);

            // Act
            var result = await _service.GetByIdAsync(feedbackId);

            // Assert
            result.Should().NotBeNull();
            result!.FeedbackId.Should().Be(feedbackId);
            result.OverallSatisfactionRating.Should().Be(5);
        }

        [Fact]
        public async Task GetByIdAsync_ShouldReturnNull_WhenNotExists()
        {
            // Arrange
            var feedbackId = Guid.NewGuid();
            _feedbackRepositoryMock.Setup(r => r.GetByIdAsync(feedbackId))
                .ReturnsAsync((FinalFeedback)null!);

            // Act
            var result = await _service.GetByIdAsync(feedbackId);

            // Assert
            result.Should().BeNull();
        }

        [Fact]
        public async Task GetAllAsync_ShouldReturnAllFeedbacks()
        {
            // Arrange
            var feedbacks = new List<FinalFeedback>
            {
                new FinalFeedback { FeedbackId = Guid.NewGuid(), OverallSatisfactionRating = 5 },
                new FinalFeedback { FeedbackId = Guid.NewGuid(), OverallSatisfactionRating = 4 }
            };

            _feedbackRepositoryMock.Setup(r => r.GetAllAsync())
                .ReturnsAsync(feedbacks);

            // Act
            var result = await _service.GetAllAsync();

            // Assert
            result.Should().HaveCount(2);
        }

        [Fact]
        public async Task GetByUserIdAsync_ShouldReturnUserFeedbacks()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var feedbacks = new List<FinalFeedback>
            {
                new FinalFeedback { FeedbackId = Guid.NewGuid(), UserId = userId },
                new FinalFeedback { FeedbackId = Guid.NewGuid(), UserId = userId }
            };

            _feedbackRepositoryMock.Setup(r => r.GetByUserIdAsync(userId))
                .ReturnsAsync(feedbacks);

            // Act
            var result = await _service.GetByUserIdAsync(userId);

            // Assert
            result.Should().HaveCount(2);
            result.Should().AllSatisfy(f => f.UserId.Should().Be(userId));
        }

        [Fact]
        public async Task GetByContractIdAsync_ShouldReturnContractFeedbacks()
        {
            // Arrange
            var contractId = Guid.NewGuid();
            var feedbacks = new List<FinalFeedback>
            {
                new FinalFeedback { FeedbackId = Guid.NewGuid(), ContractId = contractId }
            };

            _feedbackRepositoryMock.Setup(r => r.GetByContractIdAsync(contractId))
                .ReturnsAsync(feedbacks);

            // Act
            var result = await _service.GetByContractIdAsync(contractId);

            // Assert
            result.Should().HaveCount(1);
            result.First().ContractId.Should().Be(contractId);
        }

        [Fact]
        public async Task GetByContractAndProviderTypeAsync_ShouldReturnSpecificFeedback()
        {
            // Arrange
            var contractId = Guid.NewGuid();
            var providerType = "Tutor";
            var feedback = new FinalFeedback
            {
                FeedbackId = Guid.NewGuid(),
                ContractId = contractId,
                FeedbackProviderType = providerType
            };

            _feedbackRepositoryMock.Setup(r => r.GetByContractAndProviderTypeAsync(contractId, providerType))
                .ReturnsAsync(feedback);

            // Act
            var result = await _service.GetByContractAndProviderTypeAsync(contractId, providerType);

            // Assert
            result.Should().NotBeNull();
            result!.ContractId.Should().Be(contractId);
            result.FeedbackProviderType.Should().Be(providerType);
        }

        [Fact]
        public async Task GetByProviderTypeAsync_ShouldReturnFeedbacksByType()
        {
            // Arrange
            var providerType = "Parent";
            var feedbacks = new List<FinalFeedback>
            {
                new FinalFeedback { FeedbackId = Guid.NewGuid(), FeedbackProviderType = providerType },
                new FinalFeedback { FeedbackId = Guid.NewGuid(), FeedbackProviderType = providerType }
            };

            _feedbackRepositoryMock.Setup(r => r.GetByProviderTypeAsync(providerType))
                .ReturnsAsync(feedbacks);

            // Act
            var result = await _service.GetByProviderTypeAsync(providerType);

            // Assert
            result.Should().HaveCount(2);
            result.Should().AllSatisfy(f => f.FeedbackProviderType.Should().Be(providerType));
        }

        [Fact]
        public async Task GetByStatusAsync_ShouldReturnFeedbacksByStatus()
        {
            // Arrange
            var status = "Approved";
            var feedbacks = new List<FinalFeedback>
            {
                new FinalFeedback { FeedbackId = Guid.NewGuid(), FeedbackStatus = status }
            };

            _feedbackRepositoryMock.Setup(r => r.GetByStatusAsync(status))
                .ReturnsAsync(feedbacks);

            // Act
            var result = await _service.GetByStatusAsync(status);

            // Assert
            result.Should().HaveCount(1);
            result.First().FeedbackStatus.Should().Be(status);
        }

        //[Fact]
        //public async Task CreateAsync_ShouldCreateFeedback()
        //{
        //    // Arrange
        //    var userId = Guid.NewGuid();
        //    var contractId = Guid.NewGuid();
        //    var request = new CreateFinalFeedbackRequest
        //    {
        //        UserId = userId,
        //        ContractId = contractId,
        //        FeedbackProviderType = "tutor",  // Parent (roleId 3) provides feedback about tutor
        //        FeedbackText = "Excellent service",
        //        OverallSatisfactionRating = 5,
        //        CommunicationRating = 5,
        //        SessionQualityRating = 5,
        //        WouldRecommend = true
        //    };

        //    // Mock user (parent with roleId 3)
        //    _userRepositoryMock.Setup(r => r.GetByIdAsync(userId))
        //        .ReturnsAsync(new User { UserId = userId, RoleId = 3 });

        //    // Mock contract
        //    _contractRepositoryMock.Setup(r => r.GetByIdAsync(contractId))
        //        .ReturnsAsync(new Contract { ContractId = contractId, ParentId = userId, MainTutorId = Guid.NewGuid() });

        //    // Mock no existing feedbacks
        //    _feedbackRepositoryMock.Setup(r => r.GetByContractIdAsync(contractId))
        //        .ReturnsAsync(new List<FinalFeedback>());

        //    _feedbackRepositoryMock.Setup(r => r.AddAsync(It.IsAny<FinalFeedback>()))
        //        .Returns(Task.CompletedTask);

        //    // Act
        //    //var result = await _service.CreateAsync(request);

        //    //// Assert
        //    //result.Should().NotBeNull();
        //    //result.FeedbackText.Should().Be("Excellent service");
        //    //result.OverallSatisfactionRating.Should().Be(5);
        //    _feedbackRepositoryMock.Verify(r => r.AddAsync(It.IsAny<FinalFeedback>()), Times.Once);
        //}

        [Fact]
        public void Constructor_ShouldNotThrowWhenRepositoryIsNull()
        {
            // The service doesn't validate null parameters in constructor
            // Act & Assert - should not throw during construction
            var action = () => new FinalFeedbackService(null!, new Mock<IContractRepository>().Object, new Mock<IUserRepository>().Object);
            action.Should().NotThrow();
        }
    }
}

