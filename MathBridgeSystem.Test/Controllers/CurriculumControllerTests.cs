using MathBridgeSystem.Api.Controllers;
using MathBridgeSystem.Application.DTOs.Curriculum;
using MathBridgeSystem.Application.DTOs.School;
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
        public async Task UpdateCurriculum_Conflict_ReturnsConflict()
        {
            // Arrange
            var curriculumId = Guid.NewGuid();
            var request = new UpdateCurriculumRequest();
            _mockCurriculumService.Setup(s => s.UpdateCurriculumAsync(curriculumId, request))
                .ThrowsAsync(new ArgumentException("Curriculum already exists"));

            // Act
            var result = await _controller.UpdateCurriculum(curriculumId, request);

            // Assert
            Assert.IsType<ConflictObjectResult>(result);
        }

        [Fact]
        public async Task UpdateCurriculum_ArgumentException_ReturnsBadRequest()
        {
            // Arrange
            var curriculumId = Guid.NewGuid();
            var request = new UpdateCurriculumRequest();
            _mockCurriculumService.Setup(s => s.UpdateCurriculumAsync(curriculumId, request))
                .ThrowsAsync(new ArgumentException("Invalid data"));

            // Act
            var result = await _controller.UpdateCurriculum(curriculumId, request);

            // Assert
            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task UpdateCurriculum_Exception_ReturnsInternalServerError()
        {
            // Arrange
            var curriculumId = Guid.NewGuid();
            var request = new UpdateCurriculumRequest();
            _mockCurriculumService.Setup(s => s.UpdateCurriculumAsync(curriculumId, request))
                .ThrowsAsync(new Exception("db error"));

            // Act
            var result = await _controller.UpdateCurriculum(curriculumId, request);

            // Assert
            var objectResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(500, objectResult.StatusCode);
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
        public async Task DeleteCurriculum_InvalidOperation_ReturnsBadRequest()
        {
            // Arrange
            var curriculumId = Guid.NewGuid();
            _mockCurriculumService.Setup(s => s.DeleteCurriculumAsync(curriculumId))
                .ThrowsAsync(new InvalidOperationException("has associated schools"));

            // Act
            var result = await _controller.DeleteCurriculum(curriculumId);

            // Assert
            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task DeleteCurriculum_Exception_ReturnsInternalServerError()
        {
            // Arrange
            var curriculumId = Guid.NewGuid();
            _mockCurriculumService.Setup(s => s.DeleteCurriculumAsync(curriculumId))
                .ThrowsAsync(new Exception("db error"));

            // Act
            var result = await _controller.DeleteCurriculum(curriculumId);

            // Assert
            var objectResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(500, objectResult.StatusCode);
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

        [Fact]
        public async Task GetCurriculumById_NotFound_ReturnsNotFound()
        {
            // Arrange
            var curriculumId = Guid.NewGuid();
            _mockCurriculumService.Setup(s => s.GetCurriculumByIdAsync(curriculumId))
                .ReturnsAsync((CurriculumDto?)null);

            // Act
            var result = await _controller.GetCurriculumById(curriculumId);

            // Assert
            Assert.IsType<NotFoundObjectResult>(result);
        }

        [Fact]
        public async Task GetCurriculumById_Exception_ReturnsInternalServerError()
        {
            // Arrange
            var curriculumId = Guid.NewGuid();
            _mockCurriculumService.Setup(s => s.GetCurriculumByIdAsync(curriculumId))
                .ThrowsAsync(new Exception("db error"));

            // Act
            var result = await _controller.GetCurriculumById(curriculumId);

            // Assert
            var objectResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(500, objectResult.StatusCode);
        }

        [Fact]
        public async Task GetAllCurricula_ReturnsOkWithPagination()
        {
            // Arrange
            var curricula = new List<CurriculumDto> {
                new CurriculumDto { CurriculumId = Guid.NewGuid() },
                new CurriculumDto { CurriculumId = Guid.NewGuid() }
            };
            _mockCurriculumService.Setup(s => s.GetAllCurriculaAsync()).ReturnsAsync(curricula);

            // Act
            var result = await _controller.GetAllCurricula(1, 1);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.NotNull(okResult.Value);
        }

        [Fact]
        public async Task GetAllCurricula_Exception_ReturnsInternalServerError()
        {
            // Arrange
            _mockCurriculumService.Setup(s => s.GetAllCurriculaAsync()).ThrowsAsync(new Exception("boom"));

            // Act
            var result = await _controller.GetAllCurricula(1, 10);

            // Assert
            var objectResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(500, objectResult.StatusCode);
        }

        [Fact]
        public async Task GetActiveCurricula_ReturnsOkWithPagination()
        {
            // Arrange
            var curricula = new List<CurriculumDto> { new CurriculumDto { CurriculumId = Guid.NewGuid() } };
            _mockCurriculumService.Setup(s => s.GetActiveCurriculaAsync()).ReturnsAsync(curricula);

            // Act
            var result = await _controller.GetActiveCurricula(1, 10);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.NotNull(okResult.Value);
        }

        [Fact]
        public async Task GetActiveCurricula_Exception_ReturnsInternalServerError()
        {
            // Arrange
            _mockCurriculumService.Setup(s => s.GetActiveCurriculaAsync()).ThrowsAsync(new Exception("boom"));

            // Act
            var result = await _controller.GetActiveCurricula(1, 10);

            // Assert
            var objectResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(500, objectResult.StatusCode);
        }

        [Fact]
        public async Task SearchCurricula_ReturnsOkWithPagination()
        {
            // Arrange
            var request = new CurriculumSearchRequest { Page = 1, PageSize = 10 };
            var results = new List<CurriculumDto> { new CurriculumDto { CurriculumId = Guid.NewGuid() } };
            _mockCurriculumService.Setup(s => s.SearchCurriculaAsync(request)).ReturnsAsync(results);
            _mockCurriculumService.Setup(s => s.GetCurriculaCountAsync(request)).ReturnsAsync(1);

            // Act
            var result = await _controller.SearchCurricula(request);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.NotNull(okResult.Value);
        }

        [Fact]
        public async Task SearchCurricula_Exception_ReturnsInternalServerError()
        {
            // Arrange
            var request = new CurriculumSearchRequest { Page = 1, PageSize = 10 };
            _mockCurriculumService.Setup(s => s.SearchCurriculaAsync(request)).ThrowsAsync(new Exception("boom"));

            // Act
            var result = await _controller.SearchCurricula(request);

            // Assert
            var objectResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(500, objectResult.StatusCode);
        }

        [Fact]
        public async Task GetCurriculumWithSchools_Success_ReturnsOk()
        {
            // Arrange
            var id = Guid.NewGuid();
            var dto = new CurriculumWithSchoolsDto { CurriculumId = id, Schools = new List<SchoolDto> { new SchoolDto { SchoolId = Guid.NewGuid(), SchoolName = "S" } } };
            _mockCurriculumService.Setup(s => s.GetCurriculumWithSchoolsAsync(id)).ReturnsAsync(dto);

            // Act
            var result = await _controller.GetCurriculumWithSchools(id);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.NotNull(okResult.Value);
        }

        [Fact]
        public async Task GetCurriculumWithSchools_NotFound_ReturnsNotFound()
        {
            // Arrange
            var id = Guid.NewGuid();
            _mockCurriculumService.Setup(s => s.GetCurriculumWithSchoolsAsync(id)).ReturnsAsync((CurriculumWithSchoolsDto?)null);

            // Act
            var result = await _controller.GetCurriculumWithSchools(id);

            // Assert
            Assert.IsType<NotFoundObjectResult>(result);
        }

        [Fact]
        public async Task GetCurriculumWithSchools_Exception_ReturnsInternalServerError()
        {
            // Arrange
            var id = Guid.NewGuid();
            _mockCurriculumService.Setup(s => s.GetCurriculumWithSchoolsAsync(id)).ThrowsAsync(new Exception("boom"));

            // Act
            var result = await _controller.GetCurriculumWithSchools(id);

            // Assert
            var objectResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(500, objectResult.StatusCode);
        }

        [Fact]
        public async Task ActivateCurriculum_Success_ReturnsOk()
        {
            // Arrange
            var id = Guid.NewGuid();
            _mockCurriculumService.Setup(s => s.ActivateCurriculumAsync(id)).Returns(Task.CompletedTask);

            // Act
            var result = await _controller.ActivateCurriculum(id);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.NotNull(okResult.Value);
        }

        [Fact]
        public async Task ActivateCurriculum_NotFound_ReturnsNotFound()
        {
            // Arrange
            var id = Guid.NewGuid();
            _mockCurriculumService.Setup(s => s.ActivateCurriculumAsync(id)).ThrowsAsync(new KeyNotFoundException());

            // Act
            var result = await _controller.ActivateCurriculum(id);

            // Assert
            Assert.IsType<NotFoundObjectResult>(result);
        }

        [Fact]
        public async Task ActivateCurriculum_Exception_ReturnsInternalServerError()
        {
            // Arrange
            var id = Guid.NewGuid();
            _mockCurriculumService.Setup(s => s.ActivateCurriculumAsync(id)).ThrowsAsync(new Exception("boom"));

            // Act
            var result = await _controller.ActivateCurriculum(id);

            // Assert
            var objectResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(500, objectResult.StatusCode);
        }

        [Fact]
        public async Task DeactivateCurriculum_Success_ReturnsOk()
        {
            // Arrange
            var id = Guid.NewGuid();
            _mockCurriculumService.Setup(s => s.DeactivateCurriculumAsync(id)).Returns(Task.CompletedTask);

            // Act
            var result = await _controller.DeactivateCurriculum(id);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.NotNull(okResult.Value);
        }

        [Fact]
        public async Task DeactivateCurriculum_NotFound_ReturnsNotFound()
        {
            // Arrange
            var id = Guid.NewGuid();
            _mockCurriculumService.Setup(s => s.DeactivateCurriculumAsync(id)).ThrowsAsync(new KeyNotFoundException());

            // Act
            var result = await _controller.DeactivateCurriculum(id);

            // Assert
            Assert.IsType<NotFoundObjectResult>(result);
        }

        [Fact]
        public async Task DeactivateCurriculum_Exception_ReturnsInternalServerError()
        {
            // Arrange
            var id = Guid.NewGuid();
            _mockCurriculumService.Setup(s => s.DeactivateCurriculumAsync(id)).ThrowsAsync(new Exception("boom"));

            // Act
            var result = await _controller.DeactivateCurriculum(id);

            // Assert
            var objectResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(500, objectResult.StatusCode);
        }
    }
}
