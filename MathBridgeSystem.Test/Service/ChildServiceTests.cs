using FluentAssertions;
using MathBridgeSystem.Application.DTOs;
using MathBridgeSystem.Application.Services;
using MathBridgeSystem.Domain.Entities;
using MathBridgeSystem.Domain.Interfaces;
using Moq;
using Xunit;

namespace MathBridgeSystem.Tests.Services
{
    public class ChildServiceTests
    {
        private readonly Mock<IChildRepository> _childRepositoryMock;
        private readonly Mock<IUserRepository> _userRepositoryMock;
        private readonly Mock<ICenterRepository> _centerRepositoryMock;
        private readonly ChildService _childService;

        public ChildServiceTests()
        {
            _childRepositoryMock = new Mock<IChildRepository>();
            _userRepositoryMock = new Mock<IUserRepository>();
            _centerRepositoryMock = new Mock<ICenterRepository>();
            _childService = new ChildService(_childRepositoryMock.Object, _userRepositoryMock.Object, _centerRepositoryMock.Object);
        }

        [Fact]
        public async Task AddChildAsync_ValidRequest_ReturnsChildId()
        {
            // Arrange
            var parentId = Guid.NewGuid();
            var request = new AddChildRequest { FullName = "Test Child", SchoolId = Guid.NewGuid(), Grade = "grade 10" };
            var parent = new User { RoleId = 3 };
            _userRepositoryMock.Setup(repo => repo.GetByIdAsync(parentId)).ReturnsAsync(parent);
            _childRepositoryMock.Setup(repo => repo.AddAsync(It.IsAny<Child>())).Returns(Task.CompletedTask);

            // Act
            var result = await _childService.AddChildAsync(parentId, request);

            // Assert
            result.Should().NotBe(Guid.Empty);
            _childRepositoryMock.Verify(repo => repo.AddAsync(It.IsAny<Child>()), Times.Once);
        }

        [Fact]
        public async Task AddChildAsync_InvalidGrade_ThrowsException()
        {
            // Arrange
            var parentId = Guid.NewGuid();
            var request = new AddChildRequest { FullName = "Test Child", SchoolId = Guid.NewGuid(), Grade = "invalid" };
            var parent = new User { RoleId = 3 };
            _userRepositoryMock.Setup(repo => repo.GetByIdAsync(parentId)).ReturnsAsync(parent);

            // Act & Assert
            Func<Task> act = () => _childService.AddChildAsync(parentId, request);
            await act.Should().ThrowAsync<Exception>().WithMessage("Invalid grade");
        }

        [Fact]
        public async Task UpdateChildAsync_ValidRequest_UpdatesChild()
        {
            // Arrange
            var id = Guid.NewGuid();
            var request = new UpdateChildRequest { FullName = "Updated Child", SchoolId = Guid.NewGuid(), Grade = "grade 11" };
            var child = new Child { ChildId = id, Status = "active" };
            _childRepositoryMock.Setup(repo => repo.GetByIdAsync(id)).ReturnsAsync(child);
            _childRepositoryMock.Setup(repo => repo.UpdateAsync(It.IsAny<Child>())).Returns(Task.CompletedTask);

            // Act
            await _childService.UpdateChildAsync(id, request);

            // Assert
            _childRepositoryMock.Verify(repo => repo.UpdateAsync(It.IsAny<Child>()), Times.Once);
        }

        [Fact]
        public async Task UpdateChildAsync_DeletedChild_ThrowsException()
        {
            // Arrange
            var id = Guid.NewGuid();
            var request = new UpdateChildRequest();
            var child = new Child { Status = "deleted" };
            _childRepositoryMock.Setup(repo => repo.GetByIdAsync(id)).ReturnsAsync(child);

            // Act & Assert
            Func<Task> act = () => _childService.UpdateChildAsync(id, request);
            await act.Should().ThrowAsync<Exception>().WithMessage("Child not found or deleted");
        }

        [Fact]
        public async Task SoftDeleteChildAsync_ActiveChild_SetsDeletedStatus()
        {
            // Arrange
            var id = Guid.NewGuid();
            var child = new Child { Status = "active" };
            _childRepositoryMock.Setup(repo => repo.GetByIdAsync(id)).ReturnsAsync(child);
            _childRepositoryMock.Setup(repo => repo.UpdateAsync(It.IsAny<Child>())).Returns(Task.CompletedTask);

            // Act
            await _childService.SoftDeleteChildAsync(id);

            // Assert
            child.Status.Should().Be("deleted");
            _childRepositoryMock.Verify(repo => repo.UpdateAsync(It.IsAny<Child>()), Times.Once);
        }

        [Fact]
        public async Task RestoreChildAsync_DeletedChild_SetsActiveStatus()
        {
            // Arrange
            var id = Guid.NewGuid();
            var child = new Child { Status = "deleted" };
            _childRepositoryMock.Setup(repo => repo.GetByIdAsync(id)).ReturnsAsync(child);
            _childRepositoryMock.Setup(repo => repo.UpdateAsync(It.IsAny<Child>())).Returns(Task.CompletedTask);

            // Act
            await _childService.RestoreChildAsync(id);

            // Assert
            child.Status.Should().Be("active");
            _childRepositoryMock.Verify(repo => repo.UpdateAsync(It.IsAny<Child>()), Times.Once);
        }

        [Fact]
        public async Task GetChildByIdAsync_ExistingChild_ReturnsDto()
        {
            // Arrange
            var id = Guid.NewGuid();
            var child = new Child { ChildId = id, FullName = "Test Child", School = new School { SchoolName = "School" }, Center = new Center { Name = "Center" }, Grade = "grade 10", Status = "active" };
            _childRepositoryMock.Setup(repo => repo.GetByIdAsync(id)).ReturnsAsync(child);

            // Act
            var result = await _childService.GetChildByIdAsync(id);

            // Assert
            result.ChildId.Should().Be(id);
            result.FullName.Should().Be("Test Child");
            result.SchoolName.Should().Be("School");
            result.CenterName.Should().Be("Center");
        }

        [Fact]
        public async Task GetChildByIdAsync_NonExisting_ThrowsException()
        {
            // Arrange
            var id = Guid.NewGuid();
            _childRepositoryMock.Setup(repo => repo.GetByIdAsync(id)).ReturnsAsync((Child)null);

            // Act & Assert
            Func<Task> act = () => _childService.GetChildByIdAsync(id);
            await act.Should().ThrowAsync<Exception>().WithMessage("Child not found");
        }

        [Fact]
        public async Task GetChildrenByParentAsync_ReturnsList()
        {
            // Arrange
            var parentId = Guid.NewGuid();
            _childRepositoryMock.Setup(repo => repo.GetByParentIdAsync(parentId)).ReturnsAsync(new List<Child> { new Child() });

            // Act
            var result = await _childService.GetChildrenByParentAsync(parentId);

            // Assert
            result.Should().HaveCount(1);
        }

        [Fact]
        public async Task GetAllChildrenAsync_ReturnsList()
        {
            // Arrange
            _childRepositoryMock.Setup(repo => repo.GetAllAsync()).ReturnsAsync(new List<Child> { new Child() });

            // Act
            var result = await _childService.GetAllChildrenAsync();

            // Assert
            result.Should().HaveCount(1);
        }

        [Fact]
        public async Task LinkCenterAsync_ValidRequest_LinksCenter()
        {
            // Arrange
            var childId = Guid.NewGuid();
            var request = new LinkCenterRequest { CenterId = Guid.NewGuid() };
            var child = new Child { Status = "active" };
            var center = new Center();
            _childRepositoryMock.Setup(repo => repo.GetByIdAsync(childId)).ReturnsAsync(child);
            _centerRepositoryMock.Setup(repo => repo.GetByIdAsync(request.CenterId)).ReturnsAsync(center);
            _childRepositoryMock.Setup(repo => repo.UpdateAsync(It.IsAny<Child>())).Returns(Task.CompletedTask);

            // Act
            await _childService.LinkCenterAsync(childId, request);

            // Assert
            child.CenterId.Should().Be(request.CenterId);
            _childRepositoryMock.Verify(repo => repo.UpdateAsync(It.IsAny<Child>()), Times.Once);
        }

        [Fact]
        public async Task GetChildContractsAsync_ReturnsList()
        {
            // Arrange
            var childId = Guid.NewGuid();
            _childRepositoryMock.Setup(repo => repo.GetContractsByChildIdAsync(childId)).ReturnsAsync(new List<Contract> { new Contract { Child = new Child { FullName = "Child" }, Package = new PaymentPackage { PackageName = "Package" }, MainTutor = new User { FullName = "Tutor" }, Center = new Center { Name = "Center" } } });

            // Act
            var result = await _childService.GetChildContractsAsync(childId);

            // Assert
            result.Should().HaveCount(1);
            result[0].ChildName.Should().Be("Child");
        }
    }
}