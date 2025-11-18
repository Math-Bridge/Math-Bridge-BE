using FluentAssertions;
using MathBridgeSystem.Application.DTOs;
using MathBridgeSystem.Application.Interfaces;
using MathBridgeSystem.Api.Controllers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using System.Security.Claims;
using Xunit;

namespace MathBridgeSystem.Tests.Controllers
{
    public class UnitControllerTests
    {
        private readonly Mock<IUnitService> _unitServiceMock;
        private readonly UnitController _controller;
        private readonly Guid _testUserId;

        public UnitControllerTests()
        {
            _unitServiceMock = new Mock<IUnitService>();
            _controller = new UnitController(_unitServiceMock.Object);
            _testUserId = Guid.NewGuid();

            // Setup user claims
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, _testUserId.ToString()),
                new Claim(ClaimTypes.Role, "admin")
            };
            var identity = new ClaimsIdentity(claims, "Test");
            var claimsPrincipal = new ClaimsPrincipal(identity);
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = claimsPrincipal }
            };
        }

        [Fact]
        public void Constructor_NullUnitService_ThrowsArgumentNullException()
        {
            // Act & Assert
            Action act = () => new UnitController(null!);
            act.Should().Throw<ArgumentNullException>();
        }

        [Fact]
        public async Task CreateUnit_ValidRequest_ReturnsCreatedAtAction()
        {
            // Arrange
            var unitId = Guid.NewGuid();
            var request = new CreateUnitRequest
            {
                CurriculumId = Guid.NewGuid(),
                UnitName = "Algebra",
                UnitDescription = "Basic Algebra",
                Credit = 3,
                IsActive = true
            };

            _unitServiceMock.Setup(s => s.CreateUnitAsync(request, _testUserId))
                .ReturnsAsync(unitId);

            // Act
            var result = await _controller.CreateUnit(request);

            // Assert
            var createdResult = result.Should().BeOfType<CreatedAtActionResult>().Subject;
            createdResult.ActionName.Should().Be("GetUnitById");
        }

        [Fact]
        public async Task CreateUnit_ArgumentException_ReturnsBadRequest()
        {
            // Arrange
            var request = new CreateUnitRequest
            {
                CurriculumId = Guid.NewGuid(),
                UnitName = "",
                IsActive = true
            };

            _unitServiceMock.Setup(s => s.CreateUnitAsync(request, _testUserId))
                .ThrowsAsync(new ArgumentException("Unit name is required."));

            // Act
            var result = await _controller.CreateUnit(request);

            // Assert
            result.Should().BeOfType<BadRequestObjectResult>();
        }

        [Fact]
        public async Task CreateUnit_CurriculumNotFound_ReturnsNotFound()
        {
            // Arrange
            var request = new CreateUnitRequest
            {
                CurriculumId = Guid.NewGuid(),
                UnitName = "Test Unit",
                IsActive = true
            };

            _unitServiceMock.Setup(s => s.CreateUnitAsync(request, _testUserId))
                .ThrowsAsync(new InvalidOperationException("Curriculum not found."));

            // Act
            var result = await _controller.CreateUnit(request);

            // Assert
            result.Should().BeOfType<NotFoundObjectResult>();
        }

        [Fact]
        public async Task CreateUnit_UnitAlreadyExists_ReturnsConflict()
        {
            // Arrange
            var request = new CreateUnitRequest
            {
                CurriculumId = Guid.NewGuid(),
                UnitName = "Duplicate Unit",
                IsActive = true
            };

            _unitServiceMock.Setup(s => s.CreateUnitAsync(request, _testUserId))
                .ThrowsAsync(new InvalidOperationException("Unit already exists in this curriculum."));

            // Act
            var result = await _controller.CreateUnit(request);

            // Assert
            result.Should().BeOfType<ConflictObjectResult>();
        }

        [Fact]
        public async Task UpdateUnit_ValidRequest_ReturnsOk()
        {
            // Arrange
            var unitId = Guid.NewGuid();
            var request = new UpdateUnitRequest
            {
                UnitName = "Updated Algebra",
                UnitDescription = "Advanced Algebra",
                UnitOrder = 2,
                Credit = 4,
                IsActive = true
            };

            _unitServiceMock.Setup(s => s.UpdateUnitAsync(unitId, request, _testUserId))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _controller.UpdateUnit(unitId, request);

            // Assert
            result.Should().BeOfType<OkObjectResult>();
            _unitServiceMock.Verify(s => s.UpdateUnitAsync(unitId, request, _testUserId), Times.Once);
        }

        [Fact]
        public async Task UpdateUnit_UnitNotFound_ReturnsNotFound()
        {
            // Arrange
            var unitId = Guid.NewGuid();
            var request = new UpdateUnitRequest
            {
                UnitName = "Test",
                UnitOrder = 1,
                IsActive = true
            };

            _unitServiceMock.Setup(s => s.UpdateUnitAsync(unitId, request, _testUserId))
                .ThrowsAsync(new InvalidOperationException("Unit not found."));

            // Act
            var result = await _controller.UpdateUnit(unitId, request);

            // Assert
            result.Should().BeOfType<NotFoundObjectResult>();
        }

        [Fact]
        public async Task UpdateUnit_UnexpectedException_ReturnsInternalServerError()
        {
            // Arrange
            var unitId = Guid.NewGuid();
            var request = new UpdateUnitRequest
            {
                UnitName = "Test",
                UnitOrder = 1,
                IsActive = true
            };

            _unitServiceMock.Setup(s => s.UpdateUnitAsync(unitId, request, _testUserId))
                .ThrowsAsync(new Exception("Unexpected error"));

            // Act
            var result = await _controller.UpdateUnit(unitId, request);

            // Assert
            var statusCodeResult = result.Should().BeOfType<ObjectResult>().Subject;
            statusCodeResult.StatusCode.Should().Be(500);
        }

        [Fact]
        public async Task DeleteUnit_Success_ReturnsNoContent()
        {
            // Arrange
            var id = Guid.NewGuid();
            _unitServiceMock.Setup(s => s.DeleteUnitAsync(id)).Returns(Task.CompletedTask);

            // Act
            var result = await _controller.DeleteUnit(id);

            // Assert
            result.Should().BeOfType<NoContentResult>();
        }

        [Fact]
        public async Task DeleteUnit_NotFound_ReturnsNotFound()
        {
            // Arrange
            var id = Guid.NewGuid();
            _unitServiceMock.Setup(s => s.DeleteUnitAsync(id)).ThrowsAsync(new InvalidOperationException("not found"));

            // Act
            var result = await _controller.DeleteUnit(id);

            // Assert
            result.Should().BeOfType<NotFoundObjectResult>();
        }

        [Fact]
        public async Task DeleteUnit_ServerError_Returns500()
        {
            // Arrange
            var id = Guid.NewGuid();
            _unitServiceMock.Setup(s => s.DeleteUnitAsync(id)).ThrowsAsync(new Exception("boom"));

            // Act
            var result = await _controller.DeleteUnit(id);

            // Assert
            var obj = result.Should().BeOfType<ObjectResult>().Subject;
            obj.StatusCode.Should().Be(500);
        }

        [Fact]
        public async Task GetUnitById_Found_ReturnsOk()
        {
            // Arrange
            var id = Guid.NewGuid();
            var dto = new UnitDto { UnitId = id, UnitName = "A" };
            _unitServiceMock.Setup(s => s.GetUnitByIdAsync(id)).ReturnsAsync(dto);

            // Act
            var result = await _controller.GetUnitById(id);

            // Assert
            var ok = result.Should().BeOfType<OkObjectResult>().Subject;
            ok.Value.Should().Be(dto);
        }

        [Fact]
        public async Task GetUnitById_NotFound_ReturnsNotFound()
        {
            // Arrange
            var id = Guid.NewGuid();
            _unitServiceMock.Setup(s => s.GetUnitByIdAsync(id)).ReturnsAsync((UnitDto?)null);

            // Act
            var result = await _controller.GetUnitById(id);

            // Assert
            result.Should().BeOfType<NotFoundObjectResult>();
        }

        [Fact]
        public async Task GetUnitById_ServerError_Returns500()
        {
            // Arrange
            var id = Guid.NewGuid();
            _unitServiceMock.Setup(s => s.GetUnitByIdAsync(id)).ThrowsAsync(new Exception("boom"));

            // Act
            var result = await _controller.GetUnitById(id);

            // Assert
            var obj = result.Should().BeOfType<ObjectResult>().Subject;
            obj.StatusCode.Should().Be(500);
        }

        [Fact]
        public async Task GetAllUnits_ReturnsOk()
        {
            // Arrange
            var list = new List<UnitDto> { new UnitDto { UnitId = Guid.NewGuid() } };
            _unitServiceMock.Setup(s => s.GetAllUnitsAsync()).ReturnsAsync(list);

            // Act
            var result = await _controller.GetAllUnits();

            // Assert
            var ok = result.Should().BeOfType<OkObjectResult>().Subject;
            ok.Value.Should().NotBeNull();
        }

        [Fact]
        public async Task GetAllUnits_ServerError_Returns500()
        {
            // Arrange
            _unitServiceMock.Setup(s => s.GetAllUnitsAsync()).ThrowsAsync(new Exception("boom"));

            // Act
            var result = await _controller.GetAllUnits();

            // Assert
            var obj = result.Should().BeOfType<ObjectResult>().Subject;
            obj.StatusCode.Should().Be(500);
        }

        [Fact]
        public async Task GetUnitsByCurriculumId_ReturnsOk()
        {
            // Arrange
            var curriculumId = Guid.NewGuid();
            var list = new List<UnitDto> { new UnitDto { UnitId = Guid.NewGuid() } };
            _unitServiceMock.Setup(s => s.GetUnitsByCurriculumIdAsync(curriculumId)).ReturnsAsync(list);

            // Act
            var result = await _controller.GetUnitsByCurriculumId(curriculumId);

            // Assert
            var ok = result.Should().BeOfType<OkObjectResult>().Subject;
            ok.Value.Should().NotBeNull();
        }

        [Fact]
        public async Task GetUnitsByCurriculumId_ServerError_Returns500()
        {
            // Arrange
            var curriculumId = Guid.NewGuid();
            _unitServiceMock.Setup(s => s.GetUnitsByCurriculumIdAsync(curriculumId)).ThrowsAsync(new Exception("boom"));

            // Act
            var result = await _controller.GetUnitsByCurriculumId(curriculumId);

            // Assert
            var obj = result.Should().BeOfType<ObjectResult>().Subject;
            obj.StatusCode.Should().Be(500);
        }

        [Fact]
        public async Task GetUnitsByContractId_ReturnsOk()
        {
            // Arrange
            var contractId = Guid.NewGuid();
            var list = new List<UnitDto> { new UnitDto { UnitId = Guid.NewGuid() } };
            _unitServiceMock.Setup(s => s.GetUnitsByContractIdAsync(contractId)).ReturnsAsync(list);

            // Act
            var result = await _controller.GetUnitsByContractId(contractId);

            // Assert
            var ok = result.Should().BeOfType<OkObjectResult>().Subject;
            ok.Value.Should().NotBeNull();
        }

        [Fact]
        public async Task GetUnitsByContractId_ServerError_Returns500()
        {
            // Arrange
            var contractId = Guid.NewGuid();
            _unitServiceMock.Setup(s => s.GetUnitsByContractIdAsync(contractId)).ThrowsAsync(new Exception("boom"));

            // Act
            var result = await _controller.GetUnitsByContractId(contractId);

            // Assert
            var obj = result.Should().BeOfType<ObjectResult>().Subject;
            obj.StatusCode.Should().Be(500);
        }

        [Fact]
        public async Task GetUnitByName_Valid_ReturnsOk()
        {
            // Arrange
            var name = "Algebra";
            var dto = new UnitDto { UnitId = Guid.NewGuid(), UnitName = name };
            _unitServiceMock.Setup(s => s.GetUnitByNameAsync(name)).ReturnsAsync(dto);

            // Act
            var result = await _controller.GetUnitByName(name);

            // Assert
            var ok = result.Should().BeOfType<OkObjectResult>().Subject;
            ok.Value.Should().Be(dto);
        }

        [Fact]
        public async Task GetUnitByName_EmptyName_ReturnsBadRequest()
        {
            // Act
            var result = await _controller.GetUnitByName("   ");

            // Assert
            result.Should().BeOfType<BadRequestObjectResult>();
        }

        [Fact]
        public async Task GetUnitByName_NotFound_ReturnsNotFound()
        {
            // Arrange
            var name = "Unknown";
            _unitServiceMock.Setup(s => s.GetUnitByNameAsync(name)).ReturnsAsync((UnitDto?)null);

            // Act
            var result = await _controller.GetUnitByName(name);

            // Assert
            result.Should().BeOfType<NotFoundObjectResult>();
        }

        [Fact]
        public async Task GetUnitByName_ServerError_Returns500()
        {
            // Arrange
            var name = "Algebra";
            _unitServiceMock.Setup(s => s.GetUnitByNameAsync(name)).ThrowsAsync(new Exception("boom"));

            // Act
            var result = await _controller.GetUnitByName(name);

            // Assert
            var obj = result.Should().BeOfType<ObjectResult>().Subject;
            obj.StatusCode.Should().Be(500);
        }
    }
}
