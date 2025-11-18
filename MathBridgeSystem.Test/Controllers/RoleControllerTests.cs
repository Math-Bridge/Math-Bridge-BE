using FluentAssertions;
using MathBridgeSystem.Api.Controllers;
using MathBridgeSystem.Application.DTOs;
using MathBridgeSystem.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace MathBridgeSystem.Tests.Controllers
{
    public class RoleControllerTests
    {
        private readonly Mock<IRoleService> _roleServiceMock;
        private readonly RoleController _controller;

        public RoleControllerTests()
        {
            _roleServiceMock = new Mock<IRoleService>();
            _controller = new RoleController(_roleServiceMock.Object);
        }

        #region Constructor Tests

        [Fact]
        public void Constructor_NullRoleService_ThrowsArgumentNullException()
        {
            Action act = () => new RoleController(null!);
            act.Should().Throw<ArgumentNullException>().WithParameterName("roleService");
        }

        #endregion

        #region CreateRole Tests

        [Fact]
        public async Task CreateRole_ValidRequest_ReturnsCreatedResult()
        {
            // Arrange
            var request = new CreateRoleRequest
            {
                RoleName = "Teacher",
                Description = "Teaching staff"
            };
            var roleId = 5;
            _roleServiceMock.Setup(s => s.CreateRoleAsync(request))
                .ReturnsAsync(roleId);

            // Act
            var result = await _controller.CreateRole(request);

            // Assert
            var createdResult = result.Should().BeOfType<CreatedAtActionResult>().Subject;
            createdResult.ActionName.Should().Be(nameof(_controller.GetRoleById));
            _roleServiceMock.Verify(s => s.CreateRoleAsync(request), Times.Once);
        }


        [Fact]
        public async Task CreateRole_DuplicateRoleName_ReturnsConflict()
        {
            // Arrange
            var request = new CreateRoleRequest
            {
                RoleName = "Admin",
                Description = "Administrator"
            };
            _roleServiceMock.Setup(s => s.CreateRoleAsync(request))
                .ThrowsAsync(new InvalidOperationException("Role already exists"));

            // Act
            var result = await _controller.CreateRole(request);

            // Assert
            result.Should().BeOfType<ConflictObjectResult>();
        }

        [Fact]
        public async Task CreateRole_InvalidArgument_ReturnsBadRequest()
        {
            // Arrange
            var request = new CreateRoleRequest
            {
                RoleName = "",
                Description = "Test"
            };
            _roleServiceMock.Setup(s => s.CreateRoleAsync(request))
                .ThrowsAsync(new ArgumentException("Role name cannot be empty"));

            // Act
            var result = await _controller.CreateRole(request);

            // Assert
            result.Should().BeOfType<BadRequestObjectResult>();
        }

        [Fact]
        public async Task CreateRole_UnexpectedException_ReturnsInternalServerError()
        {
            // Arrange
            var request = new CreateRoleRequest
            {
                RoleName = "Teacher",
                Description = "Teaching staff"
            };
            _roleServiceMock.Setup(s => s.CreateRoleAsync(request))
                .ThrowsAsync(new Exception("Database error"));

            // Act
            var result = await _controller.CreateRole(request);

            // Assert
            var statusResult = result.Should().BeOfType<ObjectResult>().Subject;
            statusResult.StatusCode.Should().Be(500);
        }

        #endregion

        #region UpdateRole Tests

        [Fact]
        public async Task UpdateRole_ValidRequest_ReturnsOkResult()
        {
            // Arrange
            var roleId = 5;
            var request = new UpdateRoleRequest
            {
                RoleName = "Senior Teacher",
                Description = "Senior teaching staff"
            };
            _roleServiceMock.Setup(s => s.UpdateRoleAsync(roleId, request))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _controller.UpdateRole(roleId, request);

            // Assert
            result.Should().BeOfType<OkObjectResult>();
            _roleServiceMock.Verify(s => s.UpdateRoleAsync(roleId, request), Times.Once);
        }


        [Fact]
        public async Task UpdateRole_RoleNotFound_ReturnsNotFound()
        {
            // Arrange
            var roleId = 999;
            var request = new UpdateRoleRequest
            {
                RoleName = "Teacher",
                Description = "Teaching staff"
            };
            _roleServiceMock.Setup(s => s.UpdateRoleAsync(roleId, request))
                .ThrowsAsync(new InvalidOperationException("Role not found"));

            // Act
            var result = await _controller.UpdateRole(roleId, request);

            // Assert
            result.Should().BeOfType<NotFoundObjectResult>();
        }

        [Fact]
        public async Task UpdateRole_DuplicateName_ReturnsConflict()
        {
            // Arrange
            var roleId = 5;
            var request = new UpdateRoleRequest
            {
                RoleName = "Admin",
                Description = "Administrator"
            };
            _roleServiceMock.Setup(s => s.UpdateRoleAsync(roleId, request))
                .ThrowsAsync(new InvalidOperationException("Role with name already exists"));

            // Act
            var result = await _controller.UpdateRole(roleId, request);

            // Assert
            result.Should().BeOfType<ConflictObjectResult>();
        }

        [Fact]
        public async Task UpdateRole_UnexpectedException_ReturnsInternalServerError()
        {
            // Arrange
            var roleId = 5;
            var request = new UpdateRoleRequest
            {
                RoleName = "Teacher",
                Description = "Teaching staff"
            };
            _roleServiceMock.Setup(s => s.UpdateRoleAsync(roleId, request))
                .ThrowsAsync(new Exception("Database error"));

            // Act
            var result = await _controller.UpdateRole(roleId, request);

            // Assert
            var statusResult = result.Should().BeOfType<ObjectResult>().Subject;
            statusResult.StatusCode.Should().Be(500);
        }

        #endregion

        #region DeleteRole Tests

        [Fact]
        public async Task DeleteRole_ValidId_ReturnsNoContent()
        {
            // Arrange
            var roleId = 5;
            _roleServiceMock.Setup(s => s.DeleteRoleAsync(roleId))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _controller.DeleteRole(roleId);

            // Assert
            result.Should().BeOfType<NoContentResult>();
            _roleServiceMock.Verify(s => s.DeleteRoleAsync(roleId), Times.Once);
        }

        [Fact]
        public async Task DeleteRole_RoleNotFound_ReturnsNotFound()
        {
            // Arrange
            var roleId = 999;
            _roleServiceMock.Setup(s => s.DeleteRoleAsync(roleId))
                .ThrowsAsync(new InvalidOperationException("Role not found"));

            // Act
            var result = await _controller.DeleteRole(roleId);

            // Assert
            result.Should().BeOfType<NotFoundObjectResult>();
        }

        [Fact]
        public async Task DeleteRole_RoleHasUsers_ReturnsBadRequest()
        {
            // Arrange
            var roleId = 3;
            _roleServiceMock.Setup(s => s.DeleteRoleAsync(roleId))
                .ThrowsAsync(new InvalidOperationException("Cannot delete role with assigned users"));

            // Act
            var result = await _controller.DeleteRole(roleId);

            // Assert
            result.Should().BeOfType<BadRequestObjectResult>();
        }

        [Fact]
        public async Task DeleteRole_UnexpectedException_ReturnsInternalServerError()
        {
            // Arrange
            var roleId = 5;
            _roleServiceMock.Setup(s => s.DeleteRoleAsync(roleId))
                .ThrowsAsync(new Exception("Database error"));

            // Act
            var result = await _controller.DeleteRole(roleId);

            // Assert
            var statusResult = result.Should().BeOfType<ObjectResult>().Subject;
            statusResult.StatusCode.Should().Be(500);
        }

        #endregion

        #region GetRoleById, GetAll, GetByName Tests

        [Fact]
        public async Task GetRoleById_ReturnsOk()
        {
            // Arrange
            var id = 1;
            var dto = new RoleDto { RoleId = id, RoleName = "Admin" };
            _roleServiceMock.Setup(s => s.GetRoleByIdAsync(id)).ReturnsAsync(dto);

            // Act
            var result = await _controller.GetRoleById(id);

            // Assert
            result.Should().BeOfType<OkObjectResult>();
        }

        [Fact]
        public async Task GetRoleById_NotFound_ReturnsNotFound()
        {
            // Arrange
            var id = 2;
            _roleServiceMock.Setup(s => s.GetRoleByIdAsync(id)).ReturnsAsync((RoleDto?)null);

            // Act
            var result = await _controller.GetRoleById(id);

            // Assert
            result.Should().BeOfType<NotFoundObjectResult>();
        }

        [Fact]
        public async Task GetRoleById_Exception_ReturnsInternalServerError()
        {
            // Arrange
            var id = 3;
            _roleServiceMock.Setup(s => s.GetRoleByIdAsync(id)).ThrowsAsync(new Exception("boom"));

            // Act
            var result = await _controller.GetRoleById(id);

            // Assert
            var statusResult = result.Should().BeOfType<ObjectResult>().Subject;
            statusResult.StatusCode.Should().Be(500);
        }

        [Fact]
        public async Task GetAllRoles_ReturnsOk()
        {
            // Arrange
            var roles = new List<RoleDto> { new RoleDto { RoleId = 1, RoleName = "A" } };
            _roleServiceMock.Setup(s => s.GetAllRolesAsync()).ReturnsAsync(roles);

            // Act
            var result = await _controller.GetAllRoles();

            // Assert
            result.Should().BeOfType<OkObjectResult>();
        }

        [Fact]
        public async Task GetAllRoles_Exception_ReturnsInternalServerError()
        {
            // Arrange
            _roleServiceMock.Setup(s => s.GetAllRolesAsync()).ThrowsAsync(new Exception("boom"));

            // Act
            var result = await _controller.GetAllRoles();

            // Assert
            var statusResult = result.Should().BeOfType<ObjectResult>().Subject;
            statusResult.StatusCode.Should().Be(500);
        }

        [Fact]
        public async Task GetRoleByName_ReturnsOk()
        {
            // Arrange
            var name = "Admin";
            var dto = new RoleDto { RoleId = 1, RoleName = name };
            _roleServiceMock.Setup(s => s.GetRoleByNameAsync(name)).ReturnsAsync(dto);

            // Act
            var result = await _controller.GetRoleByName(name);

            // Assert
            result.Should().BeOfType<OkObjectResult>();
        }

        [Fact]
        public async Task GetRoleByName_BadRequest_ReturnsBadRequest()
        {
            // Act
            var result = await _controller.GetRoleByName("");

            // Assert
            result.Should().BeOfType<BadRequestObjectResult>();
        }

        [Fact]
        public async Task GetRoleByName_NotFound_ReturnsNotFound()
        {
            // Arrange
            var name = "X";
            _roleServiceMock.Setup(s => s.GetRoleByNameAsync(name)).ReturnsAsync((RoleDto?)null);

            // Act
            var result = await _controller.GetRoleByName(name);

            // Assert
            result.Should().BeOfType<NotFoundObjectResult>();
        }

        [Fact]
        public async Task GetRoleByName_Exception_ReturnsInternalServerError()
        {
            // Arrange
            var name = "X";
            _roleServiceMock.Setup(s => s.GetRoleByNameAsync(name)).ThrowsAsync(new Exception("boom"));

            // Act
            var result = await _controller.GetRoleByName(name);

            // Assert
            var statusResult = result.Should().BeOfType<ObjectResult>().Subject;
            statusResult.StatusCode.Should().Be(500);
        }

        #endregion
    }
}

