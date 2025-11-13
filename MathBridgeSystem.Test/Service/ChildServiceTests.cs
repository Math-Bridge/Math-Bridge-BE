using FluentAssertions;
using MathBridgeSystem.Application.DTOs;
using MathBridgeSystem.Application.Services;
using MathBridgeSystem.Domain.Entities;
using MathBridgeSystem.Domain.Interfaces;
using Moq;
using Xunit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

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
            _childService = new ChildService(
                _childRepositoryMock.Object,
                _userRepositoryMock.Object,
                _centerRepositoryMock.Object
            );
        }

        // Test: Thêm trẻ thành công với request hợp lệ (không có center)
        [Fact]
        public async Task AddChildAsync_ValidRequest_ReturnsChildId()
        {
            var parentId = Guid.NewGuid();
            var request = new AddChildRequest { FullName = "Test Child", SchoolId = Guid.NewGuid(), Grade = "grade 10" };
            var parent = new User { RoleId = 3 }; 
            _userRepositoryMock.Setup(repo => repo.GetByIdAsync(parentId)).ReturnsAsync(parent);
            _childRepositoryMock.Setup(repo => repo.AddAsync(It.IsAny<Child>())).Returns(Task.CompletedTask);

            var result = await _childService.AddChildAsync(parentId, request);

            result.Should().NotBe(Guid.Empty);
            _childRepositoryMock.Verify(repo => repo.AddAsync(It.IsAny<Child>()), Times.Once);
        }

        // Test: Ném lỗi khi thêm trẻ nhưng không tìm thấy parent
        [Fact]
        public async Task AddChildAsync_ParentNotFound_ThrowsException()
        {
            var parentId = Guid.NewGuid();
            var request = new AddChildRequest { Grade = "grade 10" };
            _userRepositoryMock.Setup(repo => repo.GetByIdAsync(parentId)).ReturnsAsync((User)null);

            Func<Task> act = () => _childService.AddChildAsync(parentId, request);
            await act.Should().ThrowAsync<Exception>().WithMessage("Invalid parent");
        }

        // Test: Ném lỗi khi thêm trẻ nhưng user không phải là parent (sai RoleId)
        [Fact]
        public async Task AddChildAsync_UserIsNotParent_ThrowsException()
        {
            var parentId = Guid.NewGuid();
            var request = new AddChildRequest { Grade = "grade 10" };
            var notAParent = new User { RoleId = 1 }; 
            _userRepositoryMock.Setup(repo => repo.GetByIdAsync(parentId)).ReturnsAsync(notAParent);

            Func<Task> act = () => _childService.AddChildAsync(parentId, request);
            await act.Should().ThrowAsync<Exception>().WithMessage("Invalid parent");
        }

        // Test: Ném lỗi khi thêm trẻ với CenterId không hợp lệ (không tìm thấy center)
        [Fact]
        public async Task AddChildAsync_InvalidCenterId_ThrowsException()
        {
            var parentId = Guid.NewGuid();
            var centerId = Guid.NewGuid();
            var request = new AddChildRequest { Grade = "grade 10", CenterId = centerId };
            var parent = new User { RoleId = 3 };
            _userRepositoryMock.Setup(repo => repo.GetByIdAsync(parentId)).ReturnsAsync(parent);
            _centerRepositoryMock.Setup(repo => repo.GetByIdAsync(centerId)).ReturnsAsync((Center)null);

            Func<Task> act = () => _childService.AddChildAsync(parentId, request);
            await act.Should().ThrowAsync<Exception>();
        }

        // Test: Thêm trẻ thành công (có cả CenterId hợp lệ)
        [Fact]
        public async Task AddChildAsync_ValidRequestWithCenter_AddsChild()
        {
            var parentId = Guid.NewGuid();
            var centerId = Guid.NewGuid();
            var request = new AddChildRequest { FullName = "Test Child", Grade = "grade 9", CenterId = centerId };
            var parent = new User { RoleId = 3 };
            var center = new Center { CenterId = centerId };

            _userRepositoryMock.Setup(repo => repo.GetByIdAsync(parentId)).ReturnsAsync(parent);
            _centerRepositoryMock.Setup(repo => repo.GetByIdAsync(centerId)).ReturnsAsync(center);

            await _childService.AddChildAsync(parentId, request);

            _childRepositoryMock.Verify(repo => repo.AddAsync(It.Is<Child>(c => c.CenterId == centerId)), Times.Once);
        }

        // Test: Ném lỗi khi thêm trẻ với khối lớp (grade) không hợp lệ
        [Fact]
        public async Task AddChildAsync_InvalidGrade_ThrowsException()
        {
            var parentId = Guid.NewGuid();
            var request = new AddChildRequest { FullName = "Test Child", SchoolId = Guid.NewGuid(), Grade = "invalid" };
            var parent = new User { RoleId = 3 };
            _userRepositoryMock.Setup(repo => repo.GetByIdAsync(parentId)).ReturnsAsync(parent);

            Func<Task> act = () => _childService.AddChildAsync(parentId, request);
            await act.Should().ThrowAsync<Exception>();
        }

        // Test: Cập nhật thông tin trẻ thành công
        [Fact]
        public async Task UpdateChildAsync_ValidRequest_UpdatesChild()
        {
            var id = Guid.NewGuid();
            var request = new UpdateChildRequest { FullName = "Updated Child", SchoolId = Guid.NewGuid(), Grade = "grade 11" };
            var child = new Child { ChildId = id, Status = "active" };
            _childRepositoryMock.Setup(repo => repo.GetByIdAsync(id)).ReturnsAsync(child);
            _childRepositoryMock.Setup(repo => repo.UpdateAsync(It.IsAny<Child>())).Returns(Task.CompletedTask);

            await _childService.UpdateChildAsync(id, request);

            _childRepositoryMock.Verify(repo => repo.UpdateAsync(It.Is<Child>(c => c.FullName == "Updated Child" && c.Grade == "grade 11")), Times.Once);
        }

        // Test: Ném lỗi khi cập nhật trẻ không tìm thấy
        [Fact]
        public async Task UpdateChildAsync_ChildNotFound_ThrowsException()
        {
            var id = Guid.NewGuid();
            var request = new UpdateChildRequest { Grade = "grade 10" };
            _childRepositoryMock.Setup(repo => repo.GetByIdAsync(id)).ReturnsAsync((Child)null);

            Func<Task> act = () => _childService.UpdateChildAsync(id, request);
            await act.Should().ThrowAsync<Exception>().WithMessage("Child not found or deleted");
        }

        // Test: Ném lỗi khi cập nhật trẻ với CenterId không hợp lệ
        [Fact]
        public async Task UpdateChildAsync_InvalidCenterId_ThrowsException()
        {
            var id = Guid.NewGuid();
            var centerId = Guid.NewGuid();
            var request = new UpdateChildRequest { Grade = "grade 10", CenterId = centerId };
            var child = new Child { ChildId = id, Status = "active" };

            _childRepositoryMock.Setup(repo => repo.GetByIdAsync(id)).ReturnsAsync(child);
            _centerRepositoryMock.Setup(repo => repo.GetByIdAsync(centerId)).ReturnsAsync((Center)null);

            Func<Task> act = () => _childService.UpdateChildAsync(id, request);
            await act.Should().ThrowAsync<Exception>().WithMessage("Center not found");
        }

        // Test: Ném lỗi khi cập nhật trẻ với khối lớp (grade) không hợp lệ
        [Fact]
        public async Task UpdateChildAsync_InvalidGrade_ThrowsException()
        {
            var id = Guid.NewGuid();
            var request = new UpdateChildRequest { Grade = "invalid" };
            var child = new Child { ChildId = id, Status = "active" };
            _childRepositoryMock.Setup(repo => repo.GetByIdAsync(id)).ReturnsAsync(child);

            Func<Task> act = () => _childService.UpdateChildAsync(id, request);
            await act.Should().ThrowAsync<Exception>().WithMessage("Invalid grade");
        }

        // Test: Ném lỗi khi cập nhật trẻ đã bị xóa
        [Fact]
        public async Task UpdateChildAsync_DeletedChild_ThrowsException()
        {
            var id = Guid.NewGuid();
            var request = new UpdateChildRequest { Grade = "grade 10" };
            var child = new Child { Status = "deleted" };
            _childRepositoryMock.Setup(repo => repo.GetByIdAsync(id)).ReturnsAsync(child);

            Func<Task> act = () => _childService.UpdateChildAsync(id, request);
            await act.Should().ThrowAsync<Exception>().WithMessage("Child not found or deleted");
        }

        // Test: Xóa mềm (soft delete) trẻ thành công
        [Fact]
        public async Task SoftDeleteChildAsync_ActiveChild_SetsDeletedStatus()
        {
            var id = Guid.NewGuid();
            var child = new Child { Status = "active" };
            _childRepositoryMock.Setup(repo => repo.GetByIdAsync(id)).ReturnsAsync(child);
            _childRepositoryMock.Setup(repo => repo.UpdateAsync(It.IsAny<Child>())).Returns(Task.CompletedTask);

            await _childService.SoftDeleteChildAsync(id);

            child.Status.Should().Be("deleted");
            _childRepositoryMock.Verify(repo => repo.UpdateAsync(It.IsAny<Child>()), Times.Once);
        }

        // Test: Ném lỗi khi xóa mềm trẻ không tìm thấy
        [Fact]
        public async Task SoftDeleteChildAsync_ChildNotFound_ThrowsException()
        {
            _childRepositoryMock.Setup(repo => repo.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((Child)null);
            Func<Task> act = () => _childService.SoftDeleteChildAsync(Guid.NewGuid());
            await act.Should().ThrowAsync<Exception>().WithMessage("Child not found or already deleted");
        }

        // Test: Ném lỗi khi xóa mềm trẻ đã bị xóa
        [Fact]
        public async Task SoftDeleteChildAsync_AlreadyDeleted_ThrowsException()
        {
            var id = Guid.NewGuid();
            var child = new Child { Status = "deleted" };
            _childRepositoryMock.Setup(repo => repo.GetByIdAsync(id)).ReturnsAsync(child);
            Func<Task> act = () => _childService.SoftDeleteChildAsync(id);
            await act.Should().ThrowAsync<Exception>().WithMessage("Child not found or already deleted");
        }

        // Test: Khôi phục (restore) trẻ đã bị xóa
        [Fact]
        public async Task RestoreChildAsync_DeletedChild_SetsActiveStatus()
        {
            var id = Guid.NewGuid();
            var child = new Child { Status = "deleted" };
            _childRepositoryMock.Setup(repo => repo.GetByIdAsync(id)).ReturnsAsync(child);
            _childRepositoryMock.Setup(repo => repo.UpdateAsync(It.IsAny<Child>())).Returns(Task.CompletedTask);

            await _childService.RestoreChildAsync(id);

            child.Status.Should().Be("active");
            _childRepositoryMock.Verify(repo => repo.UpdateAsync(It.IsAny<Child>()), Times.Once);
        }

        // Test: Ném lỗi khi khôi phục trẻ không tìm thấy
        [Fact]
        public async Task RestoreChildAsync_ChildNotFound_ThrowsException()
        {
            _childRepositoryMock.Setup(repo => repo.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((Child)null);
            Func<Task> act = () => _childService.RestoreChildAsync(Guid.NewGuid());
            await act.Should().ThrowAsync<Exception>().WithMessage("Child not found or not deleted");
        }

        // Test: Ném lỗi khi khôi phục trẻ đang active
        [Fact]
        public async Task RestoreChildAsync_AlreadyActive_ThrowsException()
        {
            var id = Guid.NewGuid();
            var child = new Child { Status = "active" };
            _childRepositoryMock.Setup(repo => repo.GetByIdAsync(id)).ReturnsAsync(child);
            Func<Task> act = () => _childService.RestoreChildAsync(id);
            await act.Should().ThrowAsync<Exception>().WithMessage("Child not found or not deleted");
        }

        // Test: Lấy thông tin trẻ bằng ID thành công
        [Fact]
        public async Task GetChildByIdAsync_ExistingChild_ReturnsDto()
        {
            var id = Guid.NewGuid();
            var child = new Child { ChildId = id, FullName = "Test Child", School = new School { SchoolName = "School" }, Center = new Center { Name = "Center" }, Grade = "grade 10", Status = "active" };
            _childRepositoryMock.Setup(repo => repo.GetByIdAsync(id)).ReturnsAsync(child);

            var result = await _childService.GetChildByIdAsync(id);

            result.ChildId.Should().Be(id);
            result.FullName.Should().Be("Test Child");
            result.SchoolName.Should().Be("School");
            result.CenterName.Should().Be("Center");
        }

        // Test: Lấy thông tin trẻ (có School/Center là null)
        [Fact]
        public async Task GetChildByIdAsync_ChildWithNullNavigationProperties_ReturnsDtoWithEmptyStrings()
        {
            var id = Guid.NewGuid();
            var child = new Child { ChildId = id, FullName = "Test Child", School = null, Center = null };
            _childRepositoryMock.Setup(repo => repo.GetByIdAsync(id)).ReturnsAsync(child);

            var result = await _childService.GetChildByIdAsync(id);

            result.SchoolName.Should().Be(string.Empty); 
            result.CenterName.Should().BeNull(); 
        }

        // Test: Ném lỗi khi lấy thông tin trẻ bằng ID không tìm thấy
        [Fact]
        public async Task GetChildByIdAsync_NonExisting_ThrowsException()
        {
            var id = Guid.NewGuid();
            _childRepositoryMock.Setup(repo => repo.GetByIdAsync(id)).ReturnsAsync((Child)null);

            Func<Task> act = () => _childService.GetChildByIdAsync(id);
            await act.Should().ThrowAsync<Exception>().WithMessage("Child not found");
        }

        // Test: Lấy danh sách trẻ theo ID phụ huynh
        [Fact]
        public async Task GetChildrenByParentAsync_ReturnsList()
        {
            var parentId = Guid.NewGuid();
            _childRepositoryMock.Setup(repo => repo.GetByParentIdAsync(parentId)).ReturnsAsync(new List<Child> { new Child() });

            var result = await _childService.GetChildrenByParentAsync(parentId);

            result.Should().HaveCount(1);
        }

        // Test: Lấy danh sách trẻ theo ID phụ huynh (không có trẻ)
        [Fact]
        public async Task GetChildrenByParentAsync_NoChildren_ReturnsEmptyList()
        {
            var parentId = Guid.NewGuid();
            _childRepositoryMock.Setup(repo => repo.GetByParentIdAsync(parentId)).ReturnsAsync(new List<Child>());
            var result = await _childService.GetChildrenByParentAsync(parentId);
            result.Should().BeEmpty();
        }

        // Test: Lấy tất cả trẻ
        [Fact]
        public async Task GetAllChildrenAsync_ReturnsList()
        {
            _childRepositoryMock.Setup(repo => repo.GetAllAsync()).ReturnsAsync(new List<Child> { new Child() });

            var result = await _childService.GetAllChildrenAsync();

            result.Should().HaveCount(1);
        }

        // Test: Lấy tất cả trẻ (không có trẻ nào)
        [Fact]
        public async Task GetAllChildrenAsync_NoChildren_ReturnsEmptyList()
        {
            _childRepositoryMock.Setup(repo => repo.GetAllAsync()).ReturnsAsync(new List<Child>());
            var result = await _childService.GetAllChildrenAsync();
            result.Should().BeEmpty();
        }

        // Test: Liên kết trẻ với trung tâm thành công
        [Fact]
        public async Task LinkCenterAsync_ValidRequest_LinksCenter()
        {
            var childId = Guid.NewGuid();
            var request = new LinkCenterRequest { CenterId = Guid.NewGuid() };
            var child = new Child { Status = "active" };
            var center = new Center();
            _childRepositoryMock.Setup(repo => repo.GetByIdAsync(childId)).ReturnsAsync(child);
            _centerRepositoryMock.Setup(repo => repo.GetByIdAsync(request.CenterId)).ReturnsAsync(center);
            _childRepositoryMock.Setup(repo => repo.UpdateAsync(It.IsAny<Child>())).Returns(Task.CompletedTask);

            await _childService.LinkCenterAsync(childId, request);

            child.CenterId.Should().Be(request.CenterId);
            _childRepositoryMock.Verify(repo => repo.UpdateAsync(It.IsAny<Child>()), Times.Once);
        }

        // Test: Ném lỗi khi liên kết trung tâm (không tìm thấy trẻ)
        [Fact]
        public async Task LinkCenterAsync_ChildNotFound_ThrowsException()
        {
            var childId = Guid.NewGuid();
            var request = new LinkCenterRequest { CenterId = Guid.NewGuid() };
            _childRepositoryMock.Setup(repo => repo.GetByIdAsync(childId)).ReturnsAsync((Child)null);

            Func<Task> act = () => _childService.LinkCenterAsync(childId, request);
            await act.Should().ThrowAsync<Exception>().WithMessage("Child not found or deleted");
        }

        // Test: Ném lỗi khi liên kết trung tâm (không tìm thấy trung tâm)
        [Fact]
        public async Task LinkCenterAsync_CenterNotFound_ThrowsException()
        {
            var childId = Guid.NewGuid();
            var request = new LinkCenterRequest { CenterId = Guid.NewGuid() };
            var child = new Child { Status = "active" };

            _childRepositoryMock.Setup(repo => repo.GetByIdAsync(childId)).ReturnsAsync(child);
            _centerRepositoryMock.Setup(repo => repo.GetByIdAsync(request.CenterId)).ReturnsAsync((Center)null);

            Func<Task> act = () => _childService.LinkCenterAsync(childId, request);
            await act.Should().ThrowAsync<Exception>().WithMessage("Center not found");
        }

        // Test: Lấy danh sách hợp đồng của trẻ
        [Fact]
        public async Task GetChildContractsAsync_ReturnsList()
        {
            var childId = Guid.NewGuid();
            var contracts = new List<Contract>
            {
                new Contract
                {
                    Child = new Child { FullName = "Child" },
                    Package = new PaymentPackage { PackageName = "Package" },
                    MainTutor = new User { FullName = "Tutor" },
                    Center = new Center { Name = "Center" }
                }
            };
            _childRepositoryMock.Setup(repo => repo.GetContractsByChildIdAsync(childId)).ReturnsAsync(contracts);

            var result = await _childService.GetChildContractsAsync(childId);

            result.Should().HaveCount(1);
            result[0].ChildName.Should().Be("Child");
        }

        // Test: Lấy danh sách hợp đồng (trẻ không có hợp đồng)
        [Fact]
        public async Task GetChildContractsAsync_NoContracts_ReturnsEmptyList()
        {
            var childId = Guid.NewGuid();
            _childRepositoryMock.Setup(repo => repo.GetContractsByChildIdAsync(childId)).ReturnsAsync(new List<Contract>());
            var result = await _childService.GetChildContractsAsync(childId);
            result.Should().BeEmpty();
        }
    }
}