using FluentAssertions;
using MathBridgeSystem.Application.DTOs;
using MathBridgeSystem.Application.Services;
using MathBridgeSystem.Domain.Entities;
using MathBridgeSystem.Domain.Interfaces;
using Moq;
using Xunit;

namespace MathBridgeSystem.Tests.Services
{
    public class RoleServiceComprehensiveTests
    {
        private readonly Mock<IRoleRepository> _roleRepositoryMock;
        private readonly RoleService _roleService;

        public RoleServiceComprehensiveTests()
        {
            _roleRepositoryMock = new Mock<IRoleRepository>();
            _roleService = new RoleService(_roleRepositoryMock.Object);
        }

        #region Constructor Tests

        [Fact]
        public void Constructor_NullRepository_ThrowsArgumentNullException()
        {
            // Act & Assert
            Action act = () => new RoleService(null!);
            act.Should().Throw<ArgumentNullException>();
        }

        #endregion

        #region CreateRoleAsync Tests

        [Fact]
        public async Task CreateRoleAsync_ValidRequest_CreatesRoleSuccessfully()
        {
            // Arrange
            var request = new CreateRoleRequest
            {
                RoleName = "NewRole",
                Description = "Test Description"
            };
            _roleRepositoryMock.Setup(r => r.ExistsByNameAsync(request.RoleName)).ReturnsAsync(false);
            _roleRepositoryMock.Setup(r => r.AddAsync(It.IsAny<Role>())).Returns(Task.CompletedTask);

            // Act
            var result = await _roleService.CreateRoleAsync(request);

            // Assert
            result.Should().BeGreaterThanOrEqualTo(0);
            _roleRepositoryMock.Verify(r => r.AddAsync(It.Is<Role>(role =>
                role.RoleName == request.RoleName &&
                role.Description == request.Description
            )), Times.Once);
        }

        [Fact]
        public async Task CreateRoleAsync_EmptyRoleName_ThrowsArgumentException()
        {
            // Arrange
            var request = new CreateRoleRequest { RoleName = "", Description = "Test" };

            // Act
            Func<Task> act = async () => await _roleService.CreateRoleAsync(request);

            // Assert
            await act.Should().ThrowAsync<ArgumentException>()
                .WithMessage("Role name is required.");
        }

        [Fact]
        public async Task CreateRoleAsync_WhitespaceRoleName_ThrowsArgumentException()
        {
            // Arrange
            var request = new CreateRoleRequest { RoleName = "   ", Description = "Test" };

            // Act
            Func<Task> act = async () => await _roleService.CreateRoleAsync(request);

            // Assert
            await act.Should().ThrowAsync<ArgumentException>();
        }

        [Fact]
        public async Task CreateRoleAsync_DuplicateRoleName_ThrowsInvalidOperationException()
        {
            // Arrange
            var request = new CreateRoleRequest { RoleName = "ExistingRole", Description = "Test" };
            _roleRepositoryMock.Setup(r => r.ExistsByNameAsync(request.RoleName)).ReturnsAsync(true);

            // Act
            Func<Task> act = async () => await _roleService.CreateRoleAsync(request);

            // Assert
            await act.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("*already exists*");
        }

        [Fact]
        public async Task CreateRoleAsync_TrimRoleName_TrimsWhitespace()
        {
            // Arrange
            var request = new CreateRoleRequest
            {
                RoleName = "  RoleWithSpaces  ",
                Description = "  Description  "
            };
            _roleRepositoryMock.Setup(r => r.ExistsByNameAsync(It.IsAny<string>())).ReturnsAsync(false);
            _roleRepositoryMock.Setup(r => r.AddAsync(It.IsAny<Role>())).Returns(Task.CompletedTask);

            // Act
            await _roleService.CreateRoleAsync(request);

            // Assert
            _roleRepositoryMock.Verify(r => r.AddAsync(It.Is<Role>(role =>
                role.RoleName == "RoleWithSpaces" &&
                role.Description == "Description"
            )), Times.Once);
        }

        #endregion

        #region UpdateRoleAsync Tests

        [Fact]
        public async Task UpdateRoleAsync_ValidRequest_UpdatesRoleSuccessfully()
        {
            // Arrange
            var roleId = 1;
            var request = new UpdateRoleRequest { RoleName = "UpdatedRole", Description = "Updated Description" };
            var existingRole = new Role { RoleId = roleId, RoleName = "OldRole", Description = "Old Description" };
            
            _roleRepositoryMock.Setup(r => r.GetByIdAsync(roleId)).ReturnsAsync(existingRole);
            _roleRepositoryMock.Setup(r => r.GetByNameAsync(request.RoleName)).ReturnsAsync((Role)null!);
            _roleRepositoryMock.Setup(r => r.UpdateAsync(It.IsAny<Role>())).Returns(Task.CompletedTask);

            // Act
            await _roleService.UpdateRoleAsync(roleId, request);

            // Assert
            existingRole.RoleName.Should().Be(request.RoleName);
            existingRole.Description.Should().Be(request.Description);
            _roleRepositoryMock.Verify(r => r.UpdateAsync(existingRole), Times.Once);
        }

        [Fact]
        public async Task UpdateRoleAsync_EmptyRoleName_ThrowsArgumentException()
        {
            // Arrange
            var request = new UpdateRoleRequest { RoleName = "", Description = "Test" };

            // Act
            Func<Task> act = async () => await _roleService.UpdateRoleAsync(1, request);

            // Assert
            await act.Should().ThrowAsync<ArgumentException>()
                .WithMessage("Role name is required.");
        }

        [Fact]
        public async Task UpdateRoleAsync_RoleNotFound_ThrowsInvalidOperationException()
        {
            // Arrange
            var request = new UpdateRoleRequest { RoleName = "NewName", Description = "Test" };
            _roleRepositoryMock.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ReturnsAsync((Role)null!);

            // Act
            Func<Task> act = async () => await _roleService.UpdateRoleAsync(999, request);

            // Assert
            await act.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("Role not found.");
        }

        [Fact]
        public async Task UpdateRoleAsync_DuplicateName_ThrowsInvalidOperationException()
        {
            // Arrange
            var roleId = 1;
            var request = new UpdateRoleRequest { RoleName = "ExistingRole", Description = "Test" };
            var currentRole = new Role { RoleId = roleId, RoleName = "OldName" };
            var duplicateRole = new Role { RoleId = 2, RoleName = "ExistingRole" };

            _roleRepositoryMock.Setup(r => r.GetByIdAsync(roleId)).ReturnsAsync(currentRole);
            _roleRepositoryMock.Setup(r => r.GetByNameAsync(request.RoleName)).ReturnsAsync(duplicateRole);

            // Act
            Func<Task> act = async () => await _roleService.UpdateRoleAsync(roleId, request);

            // Assert
            await act.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("*already exists*");
        }

        [Fact]
        public async Task UpdateRoleAsync_SameRoleName_AllowsUpdate()
        {
            // Arrange
            var roleId = 1;
            var request = new UpdateRoleRequest { RoleName = "SameRole", Description = "New Description" };
            var existingRole = new Role { RoleId = roleId, RoleName = "SameRole", Description = "Old Description" };
            
            _roleRepositoryMock.Setup(r => r.GetByIdAsync(roleId)).ReturnsAsync(existingRole);
            _roleRepositoryMock.Setup(r => r.GetByNameAsync(request.RoleName)).ReturnsAsync(existingRole);
            _roleRepositoryMock.Setup(r => r.UpdateAsync(It.IsAny<Role>())).Returns(Task.CompletedTask);

            // Act
            await _roleService.UpdateRoleAsync(roleId, request);

            // Assert
            _roleRepositoryMock.Verify(r => r.UpdateAsync(existingRole), Times.Once);
        }

        #endregion

        #region DeleteRoleAsync Tests

        [Fact]
        public async Task DeleteRoleAsync_ValidId_DeletesRoleSuccessfully()
        {
            // Arrange
            var roleId = 1;
            var role = new Role { RoleId = roleId, RoleName = "TestRole", Users = new List<User>() };
            _roleRepositoryMock.Setup(r => r.GetByIdAsync(roleId)).ReturnsAsync(role);
            _roleRepositoryMock.Setup(r => r.DeleteAsync(roleId)).Returns(Task.CompletedTask);

            // Act
            await _roleService.DeleteRoleAsync(roleId);

            // Assert
            _roleRepositoryMock.Verify(r => r.DeleteAsync(roleId), Times.Once);
        }

        [Fact]
        public async Task DeleteRoleAsync_RoleNotFound_ThrowsInvalidOperationException()
        {
            // Arrange
            _roleRepositoryMock.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ReturnsAsync((Role)null!);

            // Act
            Func<Task> act = async () => await _roleService.DeleteRoleAsync(999);

            // Assert
            await act.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("Role not found.");
        }

        [Fact]
        public async Task DeleteRoleAsync_RoleHasUsers_ThrowsInvalidOperationException()
        {
            // Arrange
            var roleId = 1;
            var role = new Role
            {
                RoleId = roleId,
                RoleName = "TestRole",
                Users = new List<User> { new User { UserId = Guid.NewGuid() } }
            };
            _roleRepositoryMock.Setup(r => r.GetByIdAsync(roleId)).ReturnsAsync(role);

            // Act
            Func<Task> act = async () => await _roleService.DeleteRoleAsync(roleId);

            // Assert
            await act.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("*assigned users*");
        }

        [Fact]
        public async Task DeleteRoleAsync_RoleWithNullUsers_DeletesSuccessfully()
        {
            // Arrange
            var roleId = 1;
            var role = new Role { RoleId = roleId, RoleName = "TestRole", Users = null };
            _roleRepositoryMock.Setup(r => r.GetByIdAsync(roleId)).ReturnsAsync(role);
            _roleRepositoryMock.Setup(r => r.DeleteAsync(roleId)).Returns(Task.CompletedTask);

            // Act
            await _roleService.DeleteRoleAsync(roleId);

            // Assert
            _roleRepositoryMock.Verify(r => r.DeleteAsync(roleId), Times.Once);
        }

        #endregion

        #region GetRoleByIdAsync Tests

        [Fact]
        public async Task GetRoleByIdAsync_ExistingRole_ReturnsRoleDto()
        {
            // Arrange
            var roleId = 1;
            var role = new Role
            {
                RoleId = roleId,
                RoleName = "TestRole",
                Description = "Test Description",
                Users = new List<User> { new User(), new User() }
            };
            _roleRepositoryMock.Setup(r => r.GetByIdAsync(roleId)).ReturnsAsync(role);

            // Act
            var result = await _roleService.GetRoleByIdAsync(roleId);

            // Assert
            result.Should().NotBeNull();
            result!.RoleId.Should().Be(roleId);
            result.RoleName.Should().Be("TestRole");
            result.Description.Should().Be("Test Description");
            result.UserCount.Should().Be(2);
        }

        [Fact]
        public async Task GetRoleByIdAsync_NonExistingRole_ReturnsNull()
        {
            // Arrange
            _roleRepositoryMock.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ReturnsAsync((Role)null!);

            // Act
            var result = await _roleService.GetRoleByIdAsync(999);

            // Assert
            result.Should().BeNull();
        }

        [Fact]
        public async Task GetRoleByIdAsync_RoleWithNullUsers_ReturnsZeroUserCount()
        {
            // Arrange
            var roleId = 1;
            var role = new Role { RoleId = roleId, RoleName = "TestRole", Users = null };
            _roleRepositoryMock.Setup(r => r.GetByIdAsync(roleId)).ReturnsAsync(role);

            // Act
            var result = await _roleService.GetRoleByIdAsync(roleId);

            // Assert
            result.Should().NotBeNull();
            result!.UserCount.Should().Be(0);
        }

        #endregion

        #region GetAllRolesAsync Tests

        [Fact]
        public async Task GetAllRolesAsync_ReturnsAllRoles()
        {
            // Arrange
            var roles = new List<Role>
            {
                new Role { RoleId = 1, RoleName = "Role1", Description = "Desc1", Users = new List<User> { new User() } },
                new Role { RoleId = 2, RoleName = "Role2", Description = "Desc2", Users = new List<User>() },
                new Role { RoleId = 3, RoleName = "Role3", Description = "Desc3", Users = null }
            };
            _roleRepositoryMock.Setup(r => r.GetAllAsync()).ReturnsAsync(roles);

            // Act
            var result = await _roleService.GetAllRolesAsync();

            // Assert
            result.Should().HaveCount(3);
            result[0].UserCount.Should().Be(1);
            result[1].UserCount.Should().Be(0);
            result[2].UserCount.Should().Be(0);
        }

        [Fact]
        public async Task GetAllRolesAsync_NoRoles_ReturnsEmptyList()
        {
            // Arrange
            _roleRepositoryMock.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<Role>());

            // Act
            var result = await _roleService.GetAllRolesAsync();

            // Assert
            result.Should().BeEmpty();
        }

        #endregion

        #region GetRoleByNameAsync Tests

        [Fact]
        public async Task GetRoleByNameAsync_ExistingRole_ReturnsRoleDto()
        {
            // Arrange
            var roleName = "TestRole";
            var role = new Role
            {
                RoleId = 1,
                RoleName = roleName,
                Description = "Test Description",
                Users = new List<User> { new User() }
            };
            _roleRepositoryMock.Setup(r => r.GetByNameAsync(roleName)).ReturnsAsync(role);

            // Act
            var result = await _roleService.GetRoleByNameAsync(roleName);

            // Assert
            result.Should().NotBeNull();
            result!.RoleName.Should().Be(roleName);
            result.UserCount.Should().Be(1);
        }

        [Fact]
        public async Task GetRoleByNameAsync_NonExistingRole_ReturnsNull()
        {
            // Arrange
            _roleRepositoryMock.Setup(r => r.GetByNameAsync(It.IsAny<string>())).ReturnsAsync((Role)null!);

            // Act
            var result = await _roleService.GetRoleByNameAsync("NonExistent");

            // Assert
            result.Should().BeNull();
        }

        #endregion
    }
}
