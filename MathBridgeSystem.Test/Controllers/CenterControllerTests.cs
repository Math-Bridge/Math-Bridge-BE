using MathBridgeSystem.Api.Controllers;
using MathBridgeSystem.Application.DTOs;
using MathBridgeSystem.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;
using Assert = Xunit.Assert;

namespace MathBridgeSystem.Test.Controllers
{
    public class CenterControllerTests
    {
        private readonly Mock<ICenterService> _mockCenterService;
        private readonly Mock<ILocationService> _mockLocationService;
        private readonly CenterController _controller;

        public CenterControllerTests()
        {
            _mockCenterService = new Mock<ICenterService>();
            _mockLocationService = new Mock<ILocationService>();
            _controller = new CenterController(_mockCenterService.Object, _mockLocationService.Object);
        }

        [Fact]
        public async Task CreateCenter_ValidRequest_ReturnsCreatedResult()
        {
            // Arrange
            var request = new CreateCenterRequest { Name = "Test Center" };
            var centerId = Guid.NewGuid();
            _mockCenterService.Setup(s => s.CreateCenterAsync(request)).ReturnsAsync(centerId);

            // Act
            var result = await _controller.CreateCenter(request);

            // Assert
            var createdResult = Assert.IsType<CreatedAtActionResult>(result);
            Assert.Equal(nameof(_controller.GetCenterById), createdResult.ActionName);
            _mockCenterService.Verify(s => s.CreateCenterAsync(request), Times.Once);
        }

        [Fact]
        public async Task CreateCenter_RequiredFieldMissing_ReturnsBadRequest()
        {
            // Arrange
            var request = new CreateCenterRequest();
            _mockCenterService.Setup(s => s.CreateCenterAsync(request))
                .ThrowsAsync(new ArgumentException("Name is required"));

            // Act
            var result = await _controller.CreateCenter(request);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.NotNull(badRequestResult.Value);
        }

        [Fact]
        public async Task CreateCenter_DuplicateName_ReturnsConflict()
        {
            // Arrange
            var request = new CreateCenterRequest { Name = "Existing Center" };
            _mockCenterService.Setup(s => s.CreateCenterAsync(request))
                .ThrowsAsync(new Exception("Center with this name already exists"));

            // Act
            var result = await _controller.CreateCenter(request);

            // Assert
            var conflictResult = Assert.IsType<ConflictObjectResult>(result);
            Assert.NotNull(conflictResult.Value);
        }

        [Fact]
        public async Task CreateCenter_InvalidGoogleMaps_ReturnsBadRequest()
        {
            // Arrange
            var request = new CreateCenterRequest { Name = "Test Center" };
            _mockCenterService.Setup(s => s.CreateCenterAsync(request))
                .ThrowsAsync(new Exception("Google Maps error"));

            // Act
            var result = await _controller.CreateCenter(request);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.NotNull(badRequestResult.Value);
        }

        [Fact]
        public async Task UpdateCenter_ValidRequest_ReturnsOk()
        {
            // Arrange
            var centerId = Guid.NewGuid();
            var request = new UpdateCenterRequest { Name = "Updated Center" };
            _mockCenterService.Setup(s => s.UpdateCenterAsync(centerId, request)).Returns(Task.CompletedTask);

            // Act
            var result = await _controller.UpdateCenter(centerId, request);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.NotNull(okResult.Value);
            _mockCenterService.Verify(s => s.UpdateCenterAsync(centerId, request), Times.Once);
        }

        [Fact]
        public async Task UpdateCenter_CenterNotFound_ReturnsNotFound()
        {
            // Arrange
            var centerId = Guid.NewGuid();
            var request = new UpdateCenterRequest { Name = "Updated Center" };
            _mockCenterService.Setup(s => s.UpdateCenterAsync(centerId, request))
                .ThrowsAsync(new Exception("Center not found"));

            // Act
            var result = await _controller.UpdateCenter(centerId, request);

            // Assert
            Assert.IsType<NotFoundObjectResult>(result);
        }

        [Fact]
        public async Task UpdateCenter_DuplicateName_ReturnsConflict()
        {
            // Arrange
            var centerId = Guid.NewGuid();
            var request = new UpdateCenterRequest { Name = "Existing Center" };
            _mockCenterService.Setup(s => s.UpdateCenterAsync(centerId, request))
                .ThrowsAsync(new Exception("Center with this name already exists"));

            // Act
            var result = await _controller.UpdateCenter(centerId, request);

            // Assert
            Assert.IsType<ConflictObjectResult>(result);
        }

        [Fact]
        public async Task UpdateCenter_InvalidGoogleMaps_ReturnsBadRequest()
        {
            // Arrange
            var centerId = Guid.NewGuid();
            var request = new UpdateCenterRequest { Name = "Test Center" };
            _mockCenterService.Setup(s => s.UpdateCenterAsync(centerId, request))
                .ThrowsAsync(new Exception("Google Maps error"));

            // Act
            var result = await _controller.UpdateCenter(centerId, request);

            // Assert
            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task UpdateCenter_GenericException_ReturnsInternalServerError()
        {
            // Arrange
            var centerId = Guid.NewGuid();
            var request = new UpdateCenterRequest { Name = "Test Center" };
            _mockCenterService.Setup(s => s.UpdateCenterAsync(centerId, request))
                .ThrowsAsync(new Exception("Unexpected error"));

            // Act
            var result = await _controller.UpdateCenter(centerId, request);

            // Assert
            var statusCodeResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(500, statusCodeResult.StatusCode);
        }

        [Fact]
        public void Constructor_NullCenterService_ThrowsArgumentNullException()
        {
            // Assert
            Assert.Throws<ArgumentNullException>(() =>
                new CenterController(null!, _mockLocationService.Object));
        }

        [Fact]
        public void Constructor_NullLocationService_ThrowsArgumentNullException()
        {
            // Assert
            Assert.Throws<ArgumentNullException>(() =>
                new CenterController(_mockCenterService.Object, null!));
        }
    }
}
