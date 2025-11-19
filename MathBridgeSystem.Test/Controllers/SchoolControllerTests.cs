using MathBridgeSystem.Api.Controllers;
using MathBridgeSystem.Application.DTOs.School;
using MathBridgeSystem.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;
using Assert = Xunit.Assert;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MathBridgeSystem.Application.DTOs;

namespace MathBridgeSystem.Tests.Controllers
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
        public async Task CreateSchool_AlreadyExists_ReturnsConflict()
        {
            var request = new CreateSchoolRequest { SchoolName = "X", CurriculumId = Guid.NewGuid() };
            _mockSchoolService.Setup(s => s.CreateSchoolAsync(request))
                .ThrowsAsync(new ArgumentException("School already exists"));

            var result = await _controller.CreateSchool(request);

            Assert.IsType<ConflictObjectResult>(result);
        }

        [Fact]
        public async Task CreateSchool_NotFound_ReturnsNotFound()
        {
            var request = new CreateSchoolRequest { SchoolName = "X", CurriculumId = Guid.NewGuid() };
            _mockSchoolService.Setup(s => s.CreateSchoolAsync(request))
                .ThrowsAsync(new ArgumentException("Curriculum not found"));

            var result = await _controller.CreateSchool(request);

            Assert.IsType<NotFoundObjectResult>(result);
        }

        [Fact]
        public async Task CreateSchool_ServerError_Returns500()
        {
            var request = new CreateSchoolRequest { SchoolName = "X", CurriculumId = Guid.NewGuid() };
            _mockSchoolService.Setup(s => s.CreateSchoolAsync(request))
                .ThrowsAsync(new Exception("boom"));

            var result = await _controller.CreateSchool(request);

            var obj = Assert.IsType<ObjectResult>(result);
            Assert.Equal(500, obj.StatusCode);
        }

        [Fact]
        public async Task UpdateSchool_ValidRequest_ReturnsOk()
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
        public async Task UpdateSchool_NotFound_ReturnsNotFound()
        {
            var id = Guid.NewGuid();
            var req = new UpdateSchoolRequest { SchoolName = "A" };
            _mockSchoolService.Setup(s => s.UpdateSchoolAsync(id, req))
                .ThrowsAsync(new KeyNotFoundException("not found"));

            var result = await _controller.UpdateSchool(id, req);

            Assert.IsType<NotFoundObjectResult>(result);
        }

        [Fact]
        public async Task UpdateSchool_Conflict_ReturnsConflict()
        {
            var id = Guid.NewGuid();
            var req = new UpdateSchoolRequest { SchoolName = "A" };
            _mockSchoolService.Setup(s => s.UpdateSchoolAsync(id, req))
                .ThrowsAsync(new ArgumentException("already exists"));

            var result = await _controller.UpdateSchool(id, req);

            Assert.IsType<ConflictObjectResult>(result);
        }

        [Fact]
        public async Task UpdateSchool_BadRequest_ReturnsBadRequest()
        {
            var id = Guid.NewGuid();
            var req = new UpdateSchoolRequest { SchoolName = "A" };
            _mockSchoolService.Setup(s => s.UpdateSchoolAsync(id, req))
                .ThrowsAsync(new ArgumentException("bad request"));

            var result = await _controller.UpdateSchool(id, req);

            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task UpdateSchool_ServerError_Returns500()
        {
            var id = Guid.NewGuid();
            var req = new UpdateSchoolRequest { SchoolName = "A" };
            _mockSchoolService.Setup(s => s.UpdateSchoolAsync(id, req))
                .ThrowsAsync(new Exception("boom"));

            var result = await _controller.UpdateSchool(id, req);

            var obj = Assert.IsType<ObjectResult>(result);
            Assert.Equal(500, obj.StatusCode);
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
        public async Task DeleteSchool_InvalidOperation_ReturnsBadRequest()
        {
            var id = Guid.NewGuid();
            _mockSchoolService.Setup(s => s.DeleteSchoolAsync(id))
                .ThrowsAsync(new InvalidOperationException("Has children enrolled"));

            var result = await _controller.DeleteSchool(id);

            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task DeleteSchool_ServerError_Returns500()
        {
            var id = Guid.NewGuid();
            _mockSchoolService.Setup(s => s.DeleteSchoolAsync(id))
                .ThrowsAsync(new Exception("boom"));

            var result = await _controller.DeleteSchool(id);

            var obj = Assert.IsType<ObjectResult>(result);
            Assert.Equal(500, obj.StatusCode);
        }

        [Fact]
        public async Task GetSchoolById_Found_ReturnsOk()
        {
            var id = Guid.NewGuid();
            var dto = new SchoolDto { SchoolId = id, SchoolName = "S", CurriculumId = Guid.NewGuid(), IsActive = true };
            _mockSchoolService.Setup(s => s.GetSchoolByIdAsync(id))
                .ReturnsAsync(dto);

            var result = await _controller.GetSchoolById(id);

            var ok = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(dto, ok.Value);
        }

        [Fact]
        public async Task GetSchoolById_NotFound_ReturnsNotFound()
        {
            var id = Guid.NewGuid();
            _mockSchoolService.Setup(s => s.GetSchoolByIdAsync(id))
                .ReturnsAsync((SchoolDto?)null);

            var result = await _controller.GetSchoolById(id);

            Assert.IsType<NotFoundObjectResult>(result);
        }

        [Fact]
        public async Task GetSchoolById_ServerError_Returns500()
        {
            var id = Guid.NewGuid();
            _mockSchoolService.Setup(s => s.GetSchoolByIdAsync(id))
                .ThrowsAsync(new Exception("boom"));

            var result = await _controller.GetSchoolById(id);

            var obj = Assert.IsType<ObjectResult>(result);
            Assert.Equal(500, obj.StatusCode);
        }

        [Fact]
        public async Task GetAllSchools_ReturnsOk()
        {
            var list = new List<SchoolDto>
            {
                new SchoolDto{ SchoolId = Guid.NewGuid(), SchoolName = "A", CurriculumId = Guid.NewGuid() },
                new SchoolDto{ SchoolId = Guid.NewGuid(), SchoolName = "B", CurriculumId = Guid.NewGuid() },
                new SchoolDto{ SchoolId = Guid.NewGuid(), SchoolName = "C", CurriculumId = Guid.NewGuid() }
            };
            _mockSchoolService.Setup(s => s.GetAllSchoolsAsync())
                .ReturnsAsync(list);

            var result = await _controller.GetAllSchools();

            var ok = Assert.IsType<OkObjectResult>(result);
            Assert.NotNull(ok.Value);
        }

        [Fact]
        public async Task GetAllSchools_ServerError_Returns500()
        {
            _mockSchoolService.Setup(s => s.GetAllSchoolsAsync())
                .ThrowsAsync(new Exception("boom"));

            var result = await _controller.GetAllSchools();

            var obj = Assert.IsType<ObjectResult>(result);
            Assert.Equal(500, obj.StatusCode);
        }

        [Fact]
        public async Task GetActiveSchools_ReturnsOk()
        {
            var list = new List<SchoolDto>
            {
                new SchoolDto{ SchoolId = Guid.NewGuid(), SchoolName = "A", CurriculumId = Guid.NewGuid() }
            };
            _mockSchoolService.Setup(s => s.GetActiveSchoolsAsync())
                .ReturnsAsync(list);

            var result = await _controller.GetActiveSchools();

            var ok = Assert.IsType<OkObjectResult>(result);
            Assert.NotNull(ok.Value);
        }

        [Fact]
        public async Task GetActiveSchools_ServerError_Returns500()
        {
            _mockSchoolService.Setup(s => s.GetActiveSchoolsAsync())
                .ThrowsAsync(new Exception("boom"));

            var result = await _controller.GetActiveSchools();

            var obj = Assert.IsType<ObjectResult>(result);
            Assert.Equal(500, obj.StatusCode);
        }

        [Fact]
        public async Task SearchSchools_ReturnsOk()
        {
            var req = new SchoolSearchRequest { Page = 1, PageSize = 10 };
            var list = new List<SchoolDto> { new SchoolDto { SchoolId = Guid.NewGuid(), SchoolName = "A", CurriculumId = Guid.NewGuid() } };
            _mockSchoolService.Setup(s => s.SearchSchoolsAsync(req)).ReturnsAsync(list);
            _mockSchoolService.Setup(s => s.GetSchoolsCountAsync(req)).ReturnsAsync(1);

            var result = await _controller.SearchSchools(req);

            var ok = Assert.IsType<OkObjectResult>(result);
            Assert.NotNull(ok.Value);
        }

        [Fact]
        public async Task SearchSchools_ServerError_Returns500()
        {
            var req = new SchoolSearchRequest { Page = 1, PageSize = 10 };
            _mockSchoolService.Setup(s => s.SearchSchoolsAsync(req)).ThrowsAsync(new Exception("boom"));

            var result = await _controller.SearchSchools(req);

            var obj = Assert.IsType<ObjectResult>(result);
            Assert.Equal(500, obj.StatusCode);
        }

        [Fact]
        public async Task GetSchoolsByCurriculum_ReturnsOk()
        {
            var curriculumId = Guid.NewGuid();
            var list = new List<SchoolDto> { new SchoolDto { SchoolId = Guid.NewGuid(), SchoolName = "A", CurriculumId = curriculumId } };
            _mockSchoolService.Setup(s => s.GetSchoolsByCurriculumAsync(curriculumId)).ReturnsAsync(list);

            var result = await _controller.GetSchoolsByCurriculum(curriculumId);

            var ok = Assert.IsType<OkObjectResult>(result);
            Assert.NotNull(ok.Value);
        }

        [Fact]
        public async Task GetSchoolsByCurriculum_ServerError_Returns500()
        {
            var curriculumId = Guid.NewGuid();
            _mockSchoolService.Setup(s => s.GetSchoolsByCurriculumAsync(curriculumId)).ThrowsAsync(new Exception("boom"));

            var result = await _controller.GetSchoolsByCurriculum(curriculumId);

            var obj = Assert.IsType<ObjectResult>(result);
            Assert.Equal(500, obj.StatusCode);
        }

        [Fact]
        public async Task GetChildrenBySchool_ReturnsOk()
        {
            var id = Guid.NewGuid();
            var list = new List<ChildDto> { new ChildDto { ChildId = Guid.NewGuid(), FullName = "X", SchoolId = id, SchoolName = "S" } };
            _mockSchoolService.Setup(s => s.GetChildrenBySchoolAsync(id)).ReturnsAsync(list);

            var result = await _controller.GetChildrenBySchool(id);

            var ok = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(list, ok.Value);
        }

        [Fact]
        public async Task GetChildrenBySchool_NotFound_ReturnsNotFound()
        {
            var id = Guid.NewGuid();
            _mockSchoolService.Setup(s => s.GetChildrenBySchoolAsync(id)).ThrowsAsync(new KeyNotFoundException("not found"));

            var result = await _controller.GetChildrenBySchool(id);

            Assert.IsType<NotFoundObjectResult>(result);
        }

        [Fact]
        public async Task GetChildrenBySchool_ServerError_Returns500()
        {
            var id = Guid.NewGuid();
            _mockSchoolService.Setup(s => s.GetChildrenBySchoolAsync(id)).ThrowsAsync(new Exception("boom"));

            var result = await _controller.GetChildrenBySchool(id);

            var obj = Assert.IsType<ObjectResult>(result);
            Assert.Equal(500, obj.StatusCode);
        }

        [Fact]
        public async Task ActivateSchool_Success_ReturnsOk()
        {
            var id = Guid.NewGuid();
            _mockSchoolService.Setup(s => s.ActivateSchoolAsync(id)).Returns(Task.CompletedTask);

            var result = await _controller.ActivateSchool(id);

            var ok = Assert.IsType<OkObjectResult>(result);
            Assert.NotNull(ok.Value);
        }

        [Fact]
        public async Task ActivateSchool_NotFound_ReturnsNotFound()
        {
            var id = Guid.NewGuid();
            _mockSchoolService.Setup(s => s.ActivateSchoolAsync(id)).ThrowsAsync(new KeyNotFoundException("not found"));

            var result = await _controller.ActivateSchool(id);

            Assert.IsType<NotFoundObjectResult>(result);
        }

        [Fact]
        public async Task ActivateSchool_ServerError_Returns500()
        {
            var id = Guid.NewGuid();
            _mockSchoolService.Setup(s => s.ActivateSchoolAsync(id)).ThrowsAsync(new Exception("boom"));

            var result = await _controller.ActivateSchool(id);

            var obj = Assert.IsType<ObjectResult>(result);
            Assert.Equal(500, obj.StatusCode);
        }

        [Fact]
        public async Task DeactivateSchool_Success_ReturnsOk()
        {
            var id = Guid.NewGuid();
            _mockSchoolService.Setup(s => s.DeactivateSchoolAsync(id)).Returns(Task.CompletedTask);

            var result = await _controller.DeactivateSchool(id);

            var ok = Assert.IsType<OkObjectResult>(result);
            Assert.NotNull(ok.Value);
        }

        [Fact]
        public async Task DeactivateSchool_NotFound_ReturnsNotFound()
        {
            var id = Guid.NewGuid();
            _mockSchoolService.Setup(s => s.DeactivateSchoolAsync(id)).ThrowsAsync(new KeyNotFoundException("not found"));

            var result = await _controller.DeactivateSchool(id);

            Assert.IsType<NotFoundObjectResult>(result);
        }

        [Fact]
        public async Task DeactivateSchool_ServerError_Returns500()
        {
            var id = Guid.NewGuid();
            _mockSchoolService.Setup(s => s.DeactivateSchoolAsync(id)).ThrowsAsync(new Exception("boom"));

            var result = await _controller.DeactivateSchool(id);

            var obj = Assert.IsType<ObjectResult>(result);
            Assert.Equal(500, obj.StatusCode);
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
