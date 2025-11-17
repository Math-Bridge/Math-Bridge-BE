using MathBridgeSystem.Api.Controllers;
using MathBridgeSystem.Application.DTOs;
using MathBridgeSystem.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;
using Assert = Xunit.Assert;

namespace MathBridgeSystem.Test.Controllers
{
    public class AdminPackageControllerTests
    {
        private readonly Mock<IPackageService> _mockPackageService;
        private readonly AdminPackageController _controller;

        public AdminPackageControllerTests()
        {
            _mockPackageService = new Mock<IPackageService>();
            _controller = new AdminPackageController(_mockPackageService.Object);
        }

        [Fact]
        public async Task CreatePackage_ValidRequest_ReturnsOk()
        {
            // Arrange
            var request = new CreatePackageRequest
            {
                PackageName = "Premium Package",
                Grade = "Grade 1",
                Price = 1000,
                SessionsPerWeek = 3,
                SessionCount = 12,
                DurationDays = 30,
                MaxReschedule = 2,
                CurriculumId = Guid.NewGuid(),
                IsActive = true
            };
            var packageId = Guid.NewGuid();
            _mockPackageService.Setup(s => s.CreatePackageAsync(request))
                .ReturnsAsync(packageId);

            // Act
            var result = await _controller.CreatePackage(request);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.NotNull(okResult.Value);
            _mockPackageService.Verify(s => s.CreatePackageAsync(request), Times.Once);
        }

        [Fact]
        public async Task CreatePackage_InvalidArgument_ReturnsBadRequest()
        {
            // Arrange
            var request = new CreatePackageRequest();
            _mockPackageService.Setup(s => s.CreatePackageAsync(request))
                .ThrowsAsync(new ArgumentException("Invalid package data"));

            // Act
            var result = await _controller.CreatePackage(request);

            // Assert
            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task UpdatePackage_ValidRequest_ReturnsOk()
        {
            // Arrange
            var packageId = Guid.NewGuid();
            var request = new UpdatePackageRequest 
            { 
                PackageName = "Updated Package" 
            };
            var updatedPackage = new PaymentPackageDto { PackageId = packageId };
            _mockPackageService.Setup(s => s.UpdatePackageAsync(packageId, request))
                .ReturnsAsync(updatedPackage);

            // Act
            var result = await _controller.UpdatePackage(packageId, request);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.NotNull(okResult.Value);
            _mockPackageService.Verify(s => s.UpdatePackageAsync(packageId, request), Times.Once);
        }

        [Fact]
        public async Task DeletePackage_ValidId_ReturnsNoContent()
        {
            // Arrange
            var packageId = Guid.NewGuid();
            _mockPackageService.Setup(s => s.DeletePackageAsync(packageId))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _controller.DeletePackage(packageId);

            // Assert
            Assert.IsType<NoContentResult>(result);
            _mockPackageService.Verify(s => s.DeletePackageAsync(packageId), Times.Once);
        }

        [Fact]
        public async Task DeletePackage_NotFound_ReturnsNotFound()
        {
            // Arrange
            var packageId = Guid.NewGuid();
            _mockPackageService.Setup(s => s.DeletePackageAsync(packageId))
                .ThrowsAsync(new KeyNotFoundException("Package not found"));

            // Act
            var result = await _controller.DeletePackage(packageId);

            // Assert
            Assert.IsType<NotFoundObjectResult>(result);
        }

        [Fact]
        public void Constructor_NullPackageService_ThrowsArgumentNullException()
        {
            // Assert
            Assert.Throws<ArgumentNullException>(() =>
                new AdminPackageController(null!));
        }
    }
}
