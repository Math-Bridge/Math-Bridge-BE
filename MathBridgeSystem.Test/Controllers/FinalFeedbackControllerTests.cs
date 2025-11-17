using MathBridgeSystem.Api.Controllers;
using MathBridgeSystem.Application.DTOs.FinalFeedback;
using MathBridgeSystem.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;
using Assert = Xunit.Assert;

namespace MathBridgeSystem.Test.Controllers
{
    public class FinalFeedbackControllerTests
    {
        private readonly Mock<IFinalFeedbackService> _mockFeedbackService;
        private readonly FinalFeedbackController _controller;

        public FinalFeedbackControllerTests()
        {
            _mockFeedbackService = new Mock<IFinalFeedbackService>();
            _controller = new FinalFeedbackController(_mockFeedbackService.Object);
        }

        [Fact]
        public async Task GetById_ValidId_ReturnsOk()
        {
            // Arrange
            var feedbackId = Guid.NewGuid();
            var feedback = new FinalFeedbackDto { FeedbackId = feedbackId };
            _mockFeedbackService.Setup(s => s.GetByIdAsync(feedbackId))
                .ReturnsAsync(feedback);

            // Act
            var result = await _controller.GetById(feedbackId);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var returnValue = Assert.IsType<FinalFeedbackDto>(okResult.Value);
            Assert.Equal(feedbackId, returnValue.FeedbackId);
            _mockFeedbackService.Verify(s => s.GetByIdAsync(feedbackId), Times.Once);
        }

        [Fact]
        public async Task GetById_NotFound_ReturnsNotFound()
        {
            // Arrange
            var feedbackId = Guid.NewGuid();
            _mockFeedbackService.Setup(s => s.GetByIdAsync(feedbackId))
                .ReturnsAsync((FinalFeedbackDto?)null);

            // Act
            var result = await _controller.GetById(feedbackId);

            // Assert
            Assert.IsType<NotFoundObjectResult>(result.Result);
        }

        [Fact]
        public async Task GetAll_ValidRequest_ReturnsOk()
        {
            // Arrange
            var feedbacks = new List<FinalFeedbackDto>
            {
                new FinalFeedbackDto { FeedbackId = Guid.NewGuid() },
                new FinalFeedbackDto { FeedbackId = Guid.NewGuid() }
            };
            _mockFeedbackService.Setup(s => s.GetAllAsync())
                .ReturnsAsync(feedbacks);

            // Act
            var result = await _controller.GetAll();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var returnValue = Assert.IsType<List<FinalFeedbackDto>>(okResult.Value);
            Assert.Equal(2, returnValue.Count);
            _mockFeedbackService.Verify(s => s.GetAllAsync(), Times.Once);
        }

        [Fact]
        public async Task GetByUserId_ValidId_ReturnsOk()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var feedbacks = new List<FinalFeedbackDto>
            {
                new FinalFeedbackDto { FeedbackId = Guid.NewGuid() }
            };
            _mockFeedbackService.Setup(s => s.GetByUserIdAsync(userId))
                .ReturnsAsync(feedbacks);

            // Act
            var result = await _controller.GetByUserId(userId);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var returnValue = Assert.IsType<List<FinalFeedbackDto>>(okResult.Value);
            Assert.Single(returnValue);
            _mockFeedbackService.Verify(s => s.GetByUserIdAsync(userId), Times.Once);
        }

        [Fact]
        public async Task GetByContractId_ValidId_ReturnsOk()
        {
            // Arrange
            var contractId = Guid.NewGuid();
            var feedbacks = new List<FinalFeedbackDto>
            {
                new FinalFeedbackDto { FeedbackId = Guid.NewGuid(), ContractId = contractId }
            };
            _mockFeedbackService.Setup(s => s.GetByContractIdAsync(contractId))
                .ReturnsAsync(feedbacks);

            // Act
            var result = await _controller.GetByContractId(contractId);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var returnValue = Assert.IsType<List<FinalFeedbackDto>>(okResult.Value);
            Assert.Single(returnValue);
            Assert.Equal(contractId, returnValue[0].ContractId);
            _mockFeedbackService.Verify(s => s.GetByContractIdAsync(contractId), Times.Once);
        }

        [Fact]
        public async Task GetByContractAndProviderType_ValidRequest_ReturnsOk()
        {
            // Arrange
            var contractId = Guid.NewGuid();
            var providerType = "tutor";
            var feedback = new FinalFeedbackDto
            {
                FeedbackId = Guid.NewGuid(),
                ContractId = contractId,
                FeedbackProviderType = providerType
            };
            _mockFeedbackService.Setup(s => s.GetByContractAndProviderTypeAsync(contractId, providerType))
                .ReturnsAsync(feedback);

            // Act
            var result = await _controller.GetByContractAndProviderType(contractId, providerType);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var returnValue = Assert.IsType<FinalFeedbackDto>(okResult.Value);
            Assert.Equal(contractId, returnValue.ContractId);
            Assert.Equal(providerType, returnValue.FeedbackProviderType);
            _mockFeedbackService.Verify(s => s.GetByContractAndProviderTypeAsync(contractId, providerType), Times.Once);
        }

        [Fact]
        public async Task GetByContractAndProviderType_NotFound_ReturnsNotFound()
        {
            // Arrange
            var contractId = Guid.NewGuid();
            var providerType = "tutor";
            _mockFeedbackService.Setup(s => s.GetByContractAndProviderTypeAsync(contractId, providerType))
                .ReturnsAsync((FinalFeedbackDto?)null);

            // Act
            var result = await _controller.GetByContractAndProviderType(contractId, providerType);

            // Assert
            Assert.IsType<NotFoundObjectResult>(result.Result);
        }

        [Fact]
        public void Constructor_NullFeedbackService_ThrowsArgumentNullException()
        {
            // Assert
            Assert.Throws<ArgumentNullException>(() =>
                new FinalFeedbackController(null!));
        }
    }
}
