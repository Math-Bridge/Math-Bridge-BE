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

        #region Constructor Tests
        [Fact]
        public void Constructor_NullPackageService_ThrowsArgumentNullException()
        {
            // Assert
            Assert.Throws<ArgumentNullException>(() =>
                new AdminPackageController(null!));
        }

        [Fact]
        public void Constructor_ValidPackageService_CreatesInstance()
        {
            // Arrange & Act
            var controller = new AdminPackageController(_mockPackageService.Object);

            // Assert
            Assert.NotNull(controller);
        }
        #endregion

        #region CreatePackage Tests
        [Fact]
        public async Task CreatePackage_ValidRequest_ReturnsOkWithPackageId()
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
        public async Task CreatePackage_InvalidModelState_ReturnsBadRequest()
        {
            // Arrange
            var request = new CreatePackageRequest();
            _controller.ModelState.AddModelError("PackageName", "Package name is required");

            // Act
            var result = await _controller.CreatePackage(request);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.IsType<SerializableError>(badRequestResult.Value);
            _mockPackageService.Verify(s => s.CreatePackageAsync(It.IsAny<CreatePackageRequest>()), Times.Never);
        }

        [Fact]
        public async Task CreatePackage_ArgumentException_ReturnsBadRequestWithErrorMessage()
        {
            // Arrange
            var request = new CreatePackageRequest
            {
                PackageName = "Test Package"
            };
            var errorMessage = "Invalid package data";
            _mockPackageService.Setup(s => s.CreatePackageAsync(request))
                .ThrowsAsync(new ArgumentException(errorMessage));

            // Act
            var result = await _controller.CreatePackage(request);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.NotNull(badRequestResult.Value);
        }

        [Fact]
        public async Task CreatePackage_KeyNotFoundException_ReturnsNotFoundWithErrorMessage()
        {
            // Arrange
            var request = new CreatePackageRequest
            {
                PackageName = "Test Package",
                CurriculumId = Guid.NewGuid()
            };
            var errorMessage = "Curriculum not found";
            _mockPackageService.Setup(s => s.CreatePackageAsync(request))
                .ThrowsAsync(new KeyNotFoundException(errorMessage));

            // Act
            var result = await _controller.CreatePackage(request);

            // Assert
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
            Assert.NotNull(notFoundResult.Value);
        }

        [Fact]
        public async Task CreatePackage_GenericException_ReturnsInternalServerError()
        {
            // Arrange
            var request = new CreatePackageRequest
            {
                PackageName = "Test Package"
            };
            var errorMessage = "Database connection failed";
            _mockPackageService.Setup(s => s.CreatePackageAsync(request))
                .ThrowsAsync(new Exception(errorMessage));

            // Act
            var result = await _controller.CreatePackage(request);

            // Assert
            var statusCodeResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(500, statusCodeResult.StatusCode);
            Assert.NotNull(statusCodeResult.Value);
        }
        #endregion

        #region UpdatePackage Tests
        [Fact]
        public async Task UpdatePackage_ValidRequest_ReturnsOkWithUpdatedPackage()
        {
            // Arrange
            var packageId = Guid.NewGuid();
            var request = new UpdatePackageRequest 
            { 
                PackageName = "Updated Package",
                Price = 1500
            };
            var updatedPackage = new PaymentPackageDto 
            { 
                PackageId = packageId,
                PackageName = "Updated Package"
            };
            _mockPackageService.Setup(s => s.UpdatePackageAsync(packageId, request))
                .ReturnsAsync(updatedPackage);

            // Act
            var result = await _controller.UpdatePackage(packageId, request);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.NotNull(okResult.Value);
            var returnedPackage = Assert.IsType<PaymentPackageDto>(okResult.Value);
            Assert.Equal(packageId, returnedPackage.PackageId);
            _mockPackageService.Verify(s => s.UpdatePackageAsync(packageId, request), Times.Once);
        }

        [Fact]
        public async Task UpdatePackage_InvalidModelState_ReturnsBadRequest()
        {
            // Arrange
            var packageId = Guid.NewGuid();
            var request = new UpdatePackageRequest();
            _controller.ModelState.AddModelError("PackageName", "Package name is required");

            // Act
            var result = await _controller.UpdatePackage(packageId, request);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.IsType<SerializableError>(badRequestResult.Value);
            _mockPackageService.Verify(s => s.UpdatePackageAsync(It.IsAny<Guid>(), It.IsAny<UpdatePackageRequest>()), Times.Never);
        }

        [Fact]
        public async Task UpdatePackage_PackageNotFound_ReturnsNotFound()
        {
            // Arrange
            var packageId = Guid.NewGuid();
            var request = new UpdatePackageRequest 
            { 
                PackageName = "Updated Package" 
            };
            _mockPackageService.Setup(s => s.UpdatePackageAsync(packageId, request))
                .ThrowsAsync(new KeyNotFoundException());

            // Act
            var result = await _controller.UpdatePackage(packageId, request);

            // Assert
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
            Assert.NotNull(notFoundResult.Value);
        }

        [Fact]
        public async Task UpdatePackage_ArgumentException_ReturnsBadRequestWithErrorMessage()
        {
            // Arrange
            var packageId = Guid.NewGuid();
            var request = new UpdatePackageRequest 
            { 
                PackageName = "" 
            };
            var errorMessage = "Package name cannot be empty";
            _mockPackageService.Setup(s => s.UpdatePackageAsync(packageId, request))
                .ThrowsAsync(new ArgumentException(errorMessage));

            // Act
            var result = await _controller.UpdatePackage(packageId, request);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.NotNull(badRequestResult.Value);
        }

        [Fact]
        public async Task UpdatePackage_GenericException_ReturnsInternalServerError()
        {
            // Arrange
            var packageId = Guid.NewGuid();
            var request = new UpdatePackageRequest 
            { 
                PackageName = "Test Package" 
            };
            var errorMessage = "Database error";
            _mockPackageService.Setup(s => s.UpdatePackageAsync(packageId, request))
                .ThrowsAsync(new Exception(errorMessage));

            // Act
            var result = await _controller.UpdatePackage(packageId, request);

            // Assert
            var statusCodeResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(500, statusCodeResult.StatusCode);
            Assert.NotNull(statusCodeResult.Value);
        }
        #endregion

        #region DeletePackage Tests
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
        public async Task DeletePackage_PackageNotFound_ReturnsNotFoundWithErrorMessage()
        {
            // Arrange
            var packageId = Guid.NewGuid();
            _mockPackageService.Setup(s => s.DeletePackageAsync(packageId))
                .ThrowsAsync(new KeyNotFoundException("Package not found"));

            // Act
            var result = await _controller.DeletePackage(packageId);

            // Assert
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
            Assert.NotNull(notFoundResult.Value);
        }

        [Fact]
        public async Task DeletePackage_InvalidOperation_ReturnsConflictWithErrorMessage()
        {
            // Arrange
            var packageId = Guid.NewGuid();
            var errorMessage = "Cannot delete package with active subscriptions";
            _mockPackageService.Setup(s => s.DeletePackageAsync(packageId))
                .ThrowsAsync(new InvalidOperationException(errorMessage));

            // Act
            var result = await _controller.DeletePackage(packageId);

            // Assert
            var conflictResult = Assert.IsType<ConflictObjectResult>(result);
            Assert.NotNull(conflictResult.Value);
        }

        [Fact]
        public async Task DeletePackage_GenericException_ReturnsInternalServerError()
        {
            // Arrange
            var packageId = Guid.NewGuid();
            var errorMessage = "Unexpected error occurred";
            _mockPackageService.Setup(s => s.DeletePackageAsync(packageId))
                .ThrowsAsync(new Exception(errorMessage));

            // Act
            var result = await _controller.DeletePackage(packageId);

            // Assert
            var statusCodeResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(500, statusCodeResult.StatusCode);
            Assert.NotNull(statusCodeResult.Value);
        }
        #endregion
    }
}
