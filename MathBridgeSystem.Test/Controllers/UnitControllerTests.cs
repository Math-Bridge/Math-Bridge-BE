using FluentAssertions;
using MathBridgeSystem.Application.DTOs;
using MathBridgeSystem.Application.Interfaces;
using MathBridgeSystem.Api.Controllers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using System.Security.Claims;
using Xunit;

namespace MathBridgeSystem.Test.Controllers
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
    }
}
