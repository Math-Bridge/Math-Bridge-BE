using MathBridgeSystem.Api.Controllers;
using MathBridgeSystem.Application.DTOs.Curriculum;
using MathBridgeSystem.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;
using Assert = Xunit.Assert;

namespace MathBridgeSystem.Test.Controllers
{
    public class CurriculumControllerTests
    {
        private readonly Mock<ICurriculumService> _mockCurriculumService;
        private readonly CurriculumController _controller;

        public CurriculumControllerTests()
        {
            _mockCurriculumService = new Mock<ICurriculumService>();
            _controller = new CurriculumController(_mockCurriculumService.Object);
        }

        [Fact]
        public async Task CreateCurriculum_ValidRequest_ReturnsCreated()
        {
            // Arrange
            var request = new CreateCurriculumRequest 
            { 
                CurriculumCode = "TEST01",
                CurriculumName = "Test Curriculum",
                Grades = "1-6"
            };
            var curriculumId = Guid.NewGuid();
            _mockCurriculumService.Setup(s => s.CreateCurriculumAsync(request))
                .ReturnsAsync(curriculumId);

            // Act
            var result = await _controller.CreateCurriculum(request);

            // Assert
            var createdResult = Assert.IsType<CreatedAtActionResult>(result);
            Assert.NotNull(createdResult.Value);
            _mockCurriculumService.Verify(s => s.CreateCurriculumAsync(request), Times.Once);
        }

        [Fact]
        public async Task CreateCurriculum_AlreadyExists_ReturnsConflict()
        {
            // Arrange
            var request = new CreateCurriculumRequest();
            _mockCurriculumService.Setup(s => s.CreateCurriculumAsync(request))
                .ThrowsAsync(new ArgumentException("Curriculum already exists"));

            // Act
            var result = await _controller.CreateCurriculum(request);

            // Assert
            Assert.IsType<ConflictObjectResult>(result);
        }

        [Fact]
        public async Task CreateCurriculum_InvalidArgument_ReturnsBadRequest()
        {
            // Arrange
            var request = new CreateCurriculumRequest();
            _mockCurriculumService.Setup(s => s.CreateCurriculumAsync(request))
                .ThrowsAsync(new ArgumentException("Invalid data"));

            // Act
            var result = await _controller.CreateCurriculum(request);

            // Assert
            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task CreateCurriculum_Exception_ReturnsInternalServerError()
        {
            // Arrange
            var request = new CreateCurriculumRequest();
            _mockCurriculumService.Setup(s => s.CreateCurriculumAsync(request))
                .ThrowsAsync(new Exception("Database error"));

            // Act
            var result = await _controller.CreateCurriculum(request);

            // Assert
            var objectResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(500, objectResult.StatusCode);
        }

        [Fact]
        public async Task UpdateCurriculum_ValidRequest_ReturnsOk()
        {
            // Arrange
            var curriculumId = Guid.NewGuid();
            var request = new UpdateCurriculumRequest 
            { 
                CurriculumName = "Updated Curriculum" 
            };
            _mockCurriculumService.Setup(s => s.UpdateCurriculumAsync(curriculumId, request))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _controller.UpdateCurriculum(curriculumId, request);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.NotNull(okResult.Value);
            _mockCurriculumService.Verify(s => s.UpdateCurriculumAsync(curriculumId, request), Times.Once);
        }

        [Fact]
        public async Task UpdateCurriculum_NotFound_ReturnsNotFound()
        {
            // Arrange
            var curriculumId = Guid.NewGuid();
            var request = new UpdateCurriculumRequest();
            _mockCurriculumService.Setup(s => s.UpdateCurriculumAsync(curriculumId, request))
                .ThrowsAsync(new KeyNotFoundException("Curriculum not found"));

            // Act
            var result = await _controller.UpdateCurriculum(curriculumId, request);

            // Assert
            Assert.IsType<NotFoundObjectResult>(result);
        }

        [Fact]
        public async Task DeleteCurriculum_ValidId_ReturnsNoContent()
        {
            // Arrange
            var curriculumId = Guid.NewGuid();
            _mockCurriculumService.Setup(s => s.DeleteCurriculumAsync(curriculumId))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _controller.DeleteCurriculum(curriculumId);

            // Assert
            Assert.IsType<NoContentResult>(result);
            _mockCurriculumService.Verify(s => s.DeleteCurriculumAsync(curriculumId), Times.Once);
        }

        [Fact]
        public async Task DeleteCurriculum_NotFound_ReturnsNotFound()
        {
            // Arrange
            var curriculumId = Guid.NewGuid();
            _mockCurriculumService.Setup(s => s.DeleteCurriculumAsync(curriculumId))
                .ThrowsAsync(new KeyNotFoundException("Curriculum not found"));

            // Act
            var result = await _controller.DeleteCurriculum(curriculumId);

            // Assert
            Assert.IsType<NotFoundObjectResult>(result);
        }

        [Fact]
        public async Task GetCurriculumById_ValidId_ReturnsOk()
        {
            // Arrange
            var curriculumId = Guid.NewGuid();
            var curriculum = new CurriculumDto { CurriculumId = curriculumId };
            _mockCurriculumService.Setup(s => s.GetCurriculumByIdAsync(curriculumId))
                .ReturnsAsync(curriculum);

            // Act
            var result = await _controller.GetCurriculumById(curriculumId);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.NotNull(okResult.Value);
            _mockCurriculumService.Verify(s => s.GetCurriculumByIdAsync(curriculumId), Times.Once);
        }
        

        [Fact]
        public void Constructor_NullCurriculumService_ThrowsArgumentNullException()
        {
            // Assert
            Assert.Throws<ArgumentNullException>(() =>
                new CurriculumController(null!));
        }
    }
}
