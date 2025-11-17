using FluentAssertions;
using MathBridgeSystem.Api.Controllers;
using MathBridgeSystem.Application.DTOs.TutorSchedule;
using MathBridgeSystem.Application.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using System.Security.Claims;
using Xunit;
using Assert = Xunit.Assert;

namespace MathBridgeSystem.Test.Controllers
{
    public class TutorScheduleControllerTests
    {
        private readonly Mock<ITutorScheduleService> _mockTutorScheduleService;
        private readonly TutorScheduleController _controller;

        public TutorScheduleControllerTests()
        {
            _mockTutorScheduleService = new Mock<ITutorScheduleService>();
            _controller = new TutorScheduleController(_mockTutorScheduleService.Object);
        }

        [Fact]
        public async Task CreateAvailability_ValidRequest_ReturnsCreatedResult()
        {
            // Arrange
            var tutorId = Guid.NewGuid();
            SetupUserClaims(tutorId, "tutor");
            var request = new CreateTutorScheduleRequest { TutorId = tutorId };
            var availabilityId = Guid.NewGuid();
            _mockTutorScheduleService.Setup(s => s.CreateAvailabilityAsync(request))
                .ReturnsAsync(availabilityId);

            // Act
            var result = await _controller.CreateAvailability(request);

            // Assert
            var createdResult = Assert.IsType<CreatedAtActionResult>(result);
            Assert.Equal(nameof(_controller.GetAvailabilityById), createdResult.ActionName);
            _mockTutorScheduleService.Verify(s => s.CreateAvailabilityAsync(request), Times.Once);
        }

        [Fact]
        public async Task CreateAvailability_TutorNotOwner_ReturnsForbid()
        {
            // Arrange
            var tutorId = Guid.NewGuid();
            var differentTutorId = Guid.NewGuid();
            SetupUserClaims(tutorId, "tutor");
            var request = new CreateTutorScheduleRequest { TutorId = differentTutorId };

            // Act
            var result = await _controller.CreateAvailability(request);

            // Assert
            Assert.IsType<ForbidResult>(result);
        }

        [Fact]
        public async Task CreateAvailability_InvalidArgument_ReturnsBadRequest()
        {
            // Arrange
            var tutorId = Guid.NewGuid();
            SetupUserClaims(tutorId, "admin");
            var request = new CreateTutorScheduleRequest { TutorId = tutorId };
            _mockTutorScheduleService.Setup(s => s.CreateAvailabilityAsync(request))
                .ThrowsAsync(new ArgumentException("Invalid time range"));

            // Act
            var result = await _controller.CreateAvailability(request);

            // Assert
            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task CreateAvailability_Exception_ReturnsInternalServerError()
        {
            // Arrange
            var tutorId = Guid.NewGuid();
            SetupUserClaims(tutorId, "admin");
            var request = new CreateTutorScheduleRequest { TutorId = tutorId };
            _mockTutorScheduleService.Setup(s => s.CreateAvailabilityAsync(request))
                .ThrowsAsync(new Exception("Database error"));

            // Act
            var result = await _controller.CreateAvailability(request);

            // Assert
            var statusCodeResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(500, statusCodeResult.StatusCode);
        }

        [Fact]
        public async Task UpdateAvailability_ValidRequest_ReturnsOk()
        {
            // Arrange
            var availabilityId = Guid.NewGuid();
            var tutorId = Guid.NewGuid();
            SetupUserClaims(tutorId, "admin");
            var request = new UpdateTutorScheduleRequest();
            _mockTutorScheduleService.Setup(s => s.UpdateAvailabilityAsync(availabilityId, request))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _controller.UpdateAvailability(availabilityId, request);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.NotNull(okResult.Value);
            _mockTutorScheduleService.Verify(s => s.UpdateAvailabilityAsync(availabilityId, request), Times.Once);
        }

        [Fact]
        public async Task UpdateAvailability_NotFound_ReturnsNotFound()
        {
            // Arrange
            var availabilityId = Guid.NewGuid();
            var tutorId = Guid.NewGuid();
            SetupUserClaims(tutorId, "tutor");
            var request = new UpdateTutorScheduleRequest();
            _mockTutorScheduleService.Setup(s => s.GetAvailabilityByIdAsync(availabilityId))
                .ReturnsAsync((TutorScheduleResponse?)null);

            // Act
            var result = await _controller.UpdateAvailability(availabilityId, request);

            // Assert
            Assert.IsType<NotFoundObjectResult>(result);
        }

        [Fact]
        public void Constructor_NullTutorScheduleService_ThrowsArgumentNullException()
        {
            // Assert
            Assert.Throws<ArgumentNullException>(() =>
                new TutorScheduleController(null!));
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
