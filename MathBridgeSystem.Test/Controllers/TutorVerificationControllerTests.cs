using MathBridgeSystem.Api.Controllers;
using MathBridgeSystem.Application.DTOs.TutorVerification;
using MathBridgeSystem.Application.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using System.Security.Claims;
using Xunit;
using Assert = Xunit.Assert;

namespace MathBridgeSystem.Test.Controllers
{
    public class TutorVerificationControllerTests
    {
        private readonly Mock<ITutorVerificationService> _mockVerificationService;
        private readonly TutorVerificationController _controller;

        public TutorVerificationControllerTests()
        {
            _mockVerificationService = new Mock<ITutorVerificationService>();
            _controller = new TutorVerificationController(_mockVerificationService.Object);
        }

        [Fact]
        public async Task CreateVerification_ValidRequest_ReturnsCreatedResult()
        {
            // Arrange
            var userId = Guid.NewGuid();
            SetupUserClaims(userId, "tutor");
            var request = new CreateTutorVerificationRequest { UserId = userId };
            var verificationId = Guid.NewGuid();
            _mockVerificationService.Setup(s => s.CreateVerificationAsync(request))
                .ReturnsAsync(verificationId);

            // Act
            var result = await _controller.CreateVerification(request);

            // Assert
            var createdResult = Assert.IsType<CreatedAtActionResult>(result);
            Assert.Equal(nameof(_controller.GetVerificationById), createdResult.ActionName);
            _mockVerificationService.Verify(s => s.CreateVerificationAsync(request), Times.Once);
        }

        [Fact]
        public async Task CreateVerification_TutorNotOwner_ReturnsForbid()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var differentUserId = Guid.NewGuid();
            SetupUserClaims(userId, "tutor");
            var request = new CreateTutorVerificationRequest { UserId = differentUserId };

            // Act
            var result = await _controller.CreateVerification(request);

            // Assert
            Assert.IsType<ForbidResult>(result);
        }

        [Fact]
        public async Task CreateVerification_AlreadyExists_ReturnsConflict()
        {
            // Arrange
            var userId = Guid.NewGuid();
            SetupUserClaims(userId, "admin");
            var request = new CreateTutorVerificationRequest { UserId = userId };
            _mockVerificationService.Setup(s => s.CreateVerificationAsync(request))
                .ThrowsAsync(new ArgumentException("Verification already exists"));

            // Act
            var result = await _controller.CreateVerification(request);

            // Assert
            Assert.IsType<ConflictObjectResult>(result);
        }

        [Fact]
        public async Task CreateVerification_UserNotFound_ReturnsNotFound()
        {
            // Arrange
            var userId = Guid.NewGuid();
            SetupUserClaims(userId, "admin");
            var request = new CreateTutorVerificationRequest { UserId = userId };
            _mockVerificationService.Setup(s => s.CreateVerificationAsync(request))
                .ThrowsAsync(new KeyNotFoundException("User not found"));

            // Act
            var result = await _controller.CreateVerification(request);

            // Assert
            Assert.IsType<NotFoundObjectResult>(result);
        }

        [Fact]
        public async Task CreateVerification_InvalidArgument_ReturnsBadRequest()
        {
            // Arrange
            var userId = Guid.NewGuid();
            SetupUserClaims(userId, "admin");
            var request = new CreateTutorVerificationRequest { UserId = userId };
            _mockVerificationService.Setup(s => s.CreateVerificationAsync(request))
                .ThrowsAsync(new ArgumentException("Invalid data"));

            // Act
            var result = await _controller.CreateVerification(request);

            // Assert
            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task CreateVerification_Exception_ReturnsInternalServerError()
        {
            // Arrange
            var userId = Guid.NewGuid();
            SetupUserClaims(userId, "admin");
            var request = new CreateTutorVerificationRequest { UserId = userId };
            _mockVerificationService.Setup(s => s.CreateVerificationAsync(request))
                .ThrowsAsync(new Exception("Database error"));

            // Act
            var result = await _controller.CreateVerification(request);

            // Assert
            var statusCodeResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(500, statusCodeResult.StatusCode);
        }

        [Fact]
        public void Constructor_NullVerificationService_ThrowsArgumentNullException()
        {
            // Assert
            Assert.Throws<ArgumentNullException>(() =>
                new TutorVerificationController(null!));
        }

        private void SetupUserClaims(Guid userId, string role)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
                new Claim(ClaimTypes.Role, role)
            };
            var identity = new ClaimsIdentity(claims);
            var principal = new ClaimsPrincipal(identity);
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = principal }
            };
        }
    }
}
