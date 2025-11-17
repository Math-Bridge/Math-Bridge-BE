using MathBridgeSystem.Api.Controllers;
using MathBridgeSystem.Application.DTOs.School;
using MathBridgeSystem.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;
using Assert = Xunit.Assert;

namespace MathBridgeSystem.Test.Controllers
{
    public class SchoolControllerTests
    {
        private readonly Mock<ISchoolService> _mockSchoolService;
        private readonly SchoolController _controller;

        public SchoolControllerTests()
        {
            _mockSchoolService = new Mock<ISchoolService>();
            _controller = new SchoolController(_mockSchoolService.Object);
        }

        [Fact]
        public async Task CreateSchool_ValidRequest_ReturnsOk()
        {
            // Arrange
            var request = new CreateSchoolRequest 
            { 
                SchoolName = "Test School",
                CurriculumId = Guid.NewGuid()
            };
            var schoolId = Guid.NewGuid();
            _mockSchoolService.Setup(s => s.CreateSchoolAsync(request))
                .ReturnsAsync(schoolId);

            // Act
            var result = await _controller.CreateSchool(request);

            // Assert
            var createdResult = Assert.IsType<CreatedAtActionResult>(result);
            Assert.NotNull(createdResult.Value);
            _mockSchoolService.Verify(s => s.CreateSchoolAsync(request), Times.Once);
        }

        [Fact]
        public async Task CreateSchool_InvalidArgument_ReturnsBadRequest()
        {
            // Arrange
            var request = new CreateSchoolRequest();
            _mockSchoolService.Setup(s => s.CreateSchoolAsync(request))
                .ThrowsAsync(new ArgumentException("Invalid school data"));

            // Act
            var result = await _controller.CreateSchool(request);

            // Assert
            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task UpdateSchool_ValidRequest_ReturnsNoContent()
        {
            // Arrange
            var schoolId = Guid.NewGuid();
            var request = new UpdateSchoolRequest 
            { 
                SchoolName = "Updated School" 
            };
            _mockSchoolService.Setup(s => s.UpdateSchoolAsync(schoolId, request))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _controller.UpdateSchool(schoolId, request);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.NotNull(okResult.Value);
            _mockSchoolService.Verify(s => s.UpdateSchoolAsync(schoolId, request), Times.Once);
        }

        [Fact]
        public async Task DeleteSchool_ValidId_ReturnsNoContent()
        {
            // Arrange
            var schoolId = Guid.NewGuid();
            _mockSchoolService.Setup(s => s.DeleteSchoolAsync(schoolId))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _controller.DeleteSchool(schoolId);

            // Assert
            Assert.IsType<NoContentResult>(result);
            _mockSchoolService.Verify(s => s.DeleteSchoolAsync(schoolId), Times.Once);
        }

        [Fact]
        public async Task DeleteSchool_NotFound_ReturnsNotFound()
        {
            // Arrange
            var schoolId = Guid.NewGuid();
            _mockSchoolService.Setup(s => s.DeleteSchoolAsync(schoolId))
                .ThrowsAsync(new KeyNotFoundException("School not found"));

            // Act
            var result = await _controller.DeleteSchool(schoolId);

            // Assert
            Assert.IsType<NotFoundObjectResult>(result);
        }

        [Fact]
        public void Constructor_NullSchoolService_ThrowsArgumentNullException()
        {
            // Assert
            Assert.Throws<ArgumentNullException>(() =>
                new SchoolController(null!));
        }
    }
}
