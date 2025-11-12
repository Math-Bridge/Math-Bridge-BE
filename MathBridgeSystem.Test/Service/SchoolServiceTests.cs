using FluentAssertions;
using MathBridgeSystem.Application.DTOs.School;
using MathBridgeSystem.Application.Services;
using MathBridgeSystem.Domain.Entities;
using MathBridgeSystem.Domain.Interfaces;
using Moq;
using Xunit;
namespace MathBridgeSystem.Tests.Services
{
    public class SchoolServiceTests
    {
        private readonly Mock<ISchoolRepository> _schoolRepositoryMock;
        private readonly SchoolService _schoolService;
        private readonly Guid _curriculumId = Guid.NewGuid();
        private readonly Curriculum _curriculum;

        public SchoolServiceTests()
        {
            _schoolRepositoryMock = new Mock<ISchoolRepository>();
            _schoolService = new SchoolService(_schoolRepositoryMock.Object);
            _curriculum = new Curriculum { CurriculumId = _curriculumId, CurriculumName = "Toán 9" };
        }


        // Test: Ném lỗi nếu request là null
        [Fact]
        public async Task CreateSchoolAsync_NullRequest_ThrowsArgumentNullException()
        {
            Func<Task> act = () => _schoolService.CreateSchoolAsync(null);
            await act.Should().ThrowAsync<ArgumentNullException>().WithParameterName("request");
        }

        // Test: Ném lỗi nếu tên trường rỗng
        [Fact]
        public async Task CreateSchoolAsync_MissingSchoolName_ThrowsArgumentException()
        {
            var request = new CreateSchoolRequest { SchoolName = " ", CurriculumId = _curriculumId };
            Func<Task> act = () => _schoolService.CreateSchoolAsync(request);
            await act.Should().ThrowAsync<ArgumentException>().WithParameterName("SchoolName");
        }

        // Test: Ném lỗi nếu không tìm thấy curriculum
        [Fact]
        public async Task CreateSchoolAsync_CurriculumNotFound_ThrowsArgumentException()
        {
            var request = new CreateSchoolRequest { SchoolName = "Test School", CurriculumId = _curriculumId };
            _schoolRepositoryMock.Setup(r => r.GetCurriculumByIdAsync(_curriculumId)).ReturnsAsync((Curriculum)null);

            Func<Task> act = () => _schoolService.CreateSchoolAsync(request);
            await act.Should().ThrowAsync<ArgumentException>().WithMessage($"Curriculum with ID {_curriculumId} not found");
        }

        // Test: Ném lỗi nếu tên trường bị trùng
        [Fact]
        public async Task CreateSchoolAsync_DuplicateName_ThrowsArgumentException()
        {
            var request = new CreateSchoolRequest { SchoolName = "Duplicate School", CurriculumId = _curriculumId };
            _schoolRepositoryMock.Setup(r => r.GetCurriculumByIdAsync(_curriculumId)).ReturnsAsync(_curriculum);
            _schoolRepositoryMock.Setup(r => r.ExistsByNameAsync("Duplicate School")).ReturnsAsync(true);

            Func<Task> act = () => _schoolService.CreateSchoolAsync(request);
            await act.Should().ThrowAsync<ArgumentException>().WithMessage("*already exists*");
        }

        // Test: Tạo trường thành công
        [Fact]
        public async Task CreateSchoolAsync_ValidRequest_CreatesSchool()
        {
            var request = new CreateSchoolRequest { SchoolName = "Test School", CurriculumId = _curriculumId };
            _schoolRepositoryMock.Setup(r => r.GetCurriculumByIdAsync(_curriculumId)).ReturnsAsync(_curriculum);
            _schoolRepositoryMock.Setup(r => r.ExistsByNameAsync("Test School")).ReturnsAsync(false);
            _schoolRepositoryMock.Setup(r => r.AddAsync(It.IsAny<School>())).Returns(Task.CompletedTask);

            var result = await _schoolService.CreateSchoolAsync(request);

            result.Should().NotBe(Guid.Empty);
            _schoolRepositoryMock.Verify(r => r.AddAsync(It.Is<School>(s => s.SchoolName == "Test School")), Times.Once);
        }


        // Test: Ném lỗi khi cập nhật (request là null)
        [Fact]
        public async Task UpdateSchoolAsync_NullRequest_ThrowsArgumentNullException()
        {
            Func<Task> act = () => _schoolService.UpdateSchoolAsync(Guid.NewGuid(), null);
            await act.Should().ThrowAsync<ArgumentNullException>().WithParameterName("request");
        }

        // Test: Ném lỗi khi cập nhật (không tìm thấy trường)
        [Fact]
        public async Task UpdateSchoolAsync_SchoolNotFound_ThrowsKeyNotFoundException()
        {
            _schoolRepositoryMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((School)null);
            Func<Task> act = () => _schoolService.UpdateSchoolAsync(Guid.NewGuid(), new UpdateSchoolRequest());
            await act.Should().ThrowAsync<KeyNotFoundException>().WithMessage("*not found*");
        }

        // Test: Ném lỗi khi cập nhật (không tìm thấy curriculum mới)
        [Fact]
        public async Task UpdateSchoolAsync_NewCurriculumNotFound_ThrowsArgumentException()
        {
            var schoolId = Guid.NewGuid();
            var newCurriculumId = Guid.NewGuid();
            var request = new UpdateSchoolRequest { CurriculumId = newCurriculumId };
            var existingSchool = new School { SchoolId = schoolId, CurriculumId = _curriculumId };

            _schoolRepositoryMock.Setup(r => r.GetByIdAsync(schoolId)).ReturnsAsync(existingSchool);
            _schoolRepositoryMock.Setup(r => r.GetCurriculumByIdAsync(newCurriculumId)).ReturnsAsync((Curriculum)null); 

            Func<Task> act = () => _schoolService.UpdateSchoolAsync(schoolId, request);
            await act.Should().ThrowAsync<ArgumentException>().WithMessage($"Curriculum with ID {newCurriculumId} not found");
        }

        // Test: Ném lỗi khi cập nhật (tên mới bị trùng)
        [Fact]
        public async Task UpdateSchoolAsync_DuplicateName_ThrowsArgumentException()
        {
            var schoolId = Guid.NewGuid();
            var request = new UpdateSchoolRequest { SchoolName = "Duplicate Name" };
            var existingSchool = new School { SchoolId = schoolId, SchoolName = "Old Name" };

            _schoolRepositoryMock.Setup(r => r.GetByIdAsync(schoolId)).ReturnsAsync(existingSchool);
            _schoolRepositoryMock.Setup(r => r.ExistsByNameAsync("Duplicate Name")).ReturnsAsync(true); 

            Func<Task> act = () => _schoolService.UpdateSchoolAsync(schoolId, request);
            await act.Should().ThrowAsync<ArgumentException>().WithMessage("*already exists*");
        }

        // Test: Cập nhật thành công (tất cả các trường)
        [Fact]
        public async Task UpdateSchoolAsync_ValidChanges_UpdatesSchool()
        {
            var schoolId = Guid.NewGuid();
            var newCurriculumId = Guid.NewGuid();
            var request = new UpdateSchoolRequest
            {
                SchoolName = "New Name",
                CurriculumId = newCurriculumId,
                IsActive = false
            };
            var existingSchool = new School
            {
                SchoolId = schoolId,
                SchoolName = "Old Name",
                CurriculumId = _curriculumId,
                IsActive = true
            };

            _schoolRepositoryMock.Setup(r => r.GetByIdAsync(schoolId)).ReturnsAsync(existingSchool);
            _schoolRepositoryMock.Setup(r => r.GetCurriculumByIdAsync(newCurriculumId)).ReturnsAsync(new Curriculum());
            _schoolRepositoryMock.Setup(r => r.ExistsByNameAsync("New Name")).ReturnsAsync(false);

            await _schoolService.UpdateSchoolAsync(schoolId, request);

            existingSchool.SchoolName.Should().Be("New Name");
            existingSchool.CurriculumId.Should().Be(newCurriculumId);
            existingSchool.IsActive.Should().BeFalse();
            existingSchool.UpdatedDate.Should().NotBeNull();
            _schoolRepositoryMock.Verify(r => r.UpdateAsync(existingSchool), Times.Once);
        }

        // Test: Không cập nhật nếu không có gì thay đổi
        [Fact]
        public async Task UpdateSchoolAsync_NoChanges_DoesNotCallUpdate()
        {
            var schoolId = Guid.NewGuid();
            var request = new UpdateSchoolRequest
            {
                SchoolName = "Same Name",
                CurriculumId = _curriculumId,
                IsActive = true
            };
            var existingSchool = new School
            {
                SchoolId = schoolId,
                SchoolName = "Same Name",
                CurriculumId = _curriculumId,
                IsActive = true
            };
            _schoolRepositoryMock.Setup(r => r.GetByIdAsync(schoolId)).ReturnsAsync(existingSchool);

            await _schoolService.UpdateSchoolAsync(schoolId, request);

            _schoolRepositoryMock.Verify(r => r.UpdateAsync(It.IsAny<School>()), Times.Never);
        }


        // Test: Ném lỗi khi xóa (không tìm thấy trường)
        [Fact]
        public async Task DeleteSchoolAsync_SchoolNotFound_ThrowsKeyNotFoundException()
        {
            _schoolRepositoryMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((School)null);
            Func<Task> act = () => _schoolService.DeleteSchoolAsync(Guid.NewGuid());
            await act.Should().ThrowAsync<KeyNotFoundException>();
        }

        // Test: Ném lỗi khi xóa (trường vẫn còn trẻ em)
        [Fact]
        public async Task DeleteSchoolAsync_SchoolHasChildren_ThrowsInvalidOperationException()
        {
            var schoolId = Guid.NewGuid();
            _schoolRepositoryMock.Setup(r => r.GetByIdAsync(schoolId)).ReturnsAsync(new School());
            _schoolRepositoryMock.Setup(r => r.GetChildrenCountAsync(schoolId)).ReturnsAsync(3); 

            Func<Task> act = () => _schoolService.DeleteSchoolAsync(schoolId);
            await act.Should().ThrowAsync<InvalidOperationException>().WithMessage($"*because it has 3 enrolled children*");
        }

        // Test: Xóa thành công
        [Fact]
        public async Task DeleteSchoolAsync_Valid_DeletesSchool()
        {
            var schoolId = Guid.NewGuid();
            _schoolRepositoryMock.Setup(r => r.GetByIdAsync(schoolId)).ReturnsAsync(new School());
            _schoolRepositoryMock.Setup(r => r.GetChildrenCountAsync(schoolId)).ReturnsAsync(0); 

            await _schoolService.DeleteSchoolAsync(schoolId);

            _schoolRepositoryMock.Verify(r => r.DeleteAsync(schoolId), Times.Once);
        }


        // Test: Kích hoạt trường
        [Fact]
        public async Task ActivateSchoolAsync_InactiveSchool_Activates()
        {
            var schoolId = Guid.NewGuid();
            var school = new School { SchoolId = schoolId, IsActive = false };
            _schoolRepositoryMock.Setup(r => r.GetByIdAsync(schoolId)).ReturnsAsync(school);

            await _schoolService.ActivateSchoolAsync(schoolId);

            school.IsActive.Should().BeTrue();
            _schoolRepositoryMock.Verify(r => r.UpdateAsync(school), Times.Once);
        }

        // Test: Kích hoạt trường (đã active sẵn)
        [Fact]
        public async Task ActivateSchoolAsync_AlreadyActive_DoesNothing()
        {
            var schoolId = Guid.NewGuid();
            var school = new School { SchoolId = schoolId, IsActive = true };
            _schoolRepositoryMock.Setup(r => r.GetByIdAsync(schoolId)).ReturnsAsync(school);

            await _schoolService.ActivateSchoolAsync(schoolId);

            _schoolRepositoryMock.Verify(r => r.UpdateAsync(It.IsAny<School>()), Times.Never);
        }

        // Test: Vô hiệu hóa trường
        [Fact]
        public async Task DeactivateSchoolAsync_ActiveSchool_Deactivates()
        {
            var schoolId = Guid.NewGuid();
            var school = new School { SchoolId = schoolId, IsActive = true };
            _schoolRepositoryMock.Setup(r => r.GetByIdAsync(schoolId)).ReturnsAsync(school);

            await _schoolService.DeactivateSchoolAsync(schoolId);

            school.IsActive.Should().BeFalse();
            _schoolRepositoryMock.Verify(r => r.UpdateAsync(school), Times.Once);
        }

        // Test: Vô hiệu hóa trường (đã inactive sẵn)
        [Fact]
        public async Task DeactivateSchoolAsync_AlreadyInactive_DoesNothing()
        {
            var schoolId = Guid.NewGuid();
            var school = new School { SchoolId = schoolId, IsActive = false };
            _schoolRepositoryMock.Setup(r => r.GetByIdAsync(schoolId)).ReturnsAsync(school);

            await _schoolService.DeactivateSchoolAsync(schoolId);

            _schoolRepositoryMock.Verify(r => r.UpdateAsync(It.IsAny<School>()), Times.Never);
        }


        // Test: Lấy trường bằng ID (tìm thấy)
        [Fact]
        public async Task GetSchoolByIdAsync_Found_ReturnsDto()
        {
            var schoolId = Guid.NewGuid();
            var school = new School { SchoolId = schoolId, SchoolName = "Test School", Curriculum = _curriculum };
            _schoolRepositoryMock.Setup(r => r.GetByIdAsync(schoolId)).ReturnsAsync(school);
            _schoolRepositoryMock.Setup(r => r.GetChildrenCountAsync(schoolId)).ReturnsAsync(5);

            var result = await _schoolService.GetSchoolByIdAsync(schoolId);

            result.Should().NotBeNull();
            result.SchoolName.Should().Be("Test School");
            result.TotalChildren.Should().Be(5);
            result.CurriculumName.Should().Be("Toán 9");
        }

        // Test: Lấy trường bằng ID (không tìm thấy)
        [Fact]
        public async Task GetSchoolByIdAsync_NotFound_ReturnsNull()
        {
            _schoolRepositoryMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((School)null);
            var result = await _schoolService.GetSchoolByIdAsync(Guid.NewGuid());
            result.Should().BeNull();
        }

        // Test: Lấy tất cả trường (kiểm tra N+1 query)
        [Fact]
        public async Task GetAllSchoolsAsync_CallsGetChildrenCountPerSchool()
        {
            var schools = new List<School>
            {
                new School { SchoolId = Guid.NewGuid() },
                new School { SchoolId = Guid.NewGuid() }
            };
            _schoolRepositoryMock.Setup(r => r.GetAllAsync()).ReturnsAsync(schools);
            _schoolRepositoryMock.Setup(r => r.GetChildrenCountAsync(It.IsAny<Guid>())).ReturnsAsync(1); 

            var result = await _schoolService.GetAllSchoolsAsync();

            result.Should().HaveCount(2);

            _schoolRepositoryMock.Verify(r => r.GetChildrenCountAsync(It.IsAny<Guid>()), Times.Exactly(2));
        }

        // Test: Lấy các trường active
        [Fact]
        public async Task GetActiveSchoolsAsync_ReturnsActiveSchools()
        {
            var schools = new List<School> { new School { SchoolId = Guid.NewGuid(), IsActive = true } };
            _schoolRepositoryMock.Setup(r => r.GetActiveSchoolsAsync()).ReturnsAsync(schools);
            _schoolRepositoryMock.Setup(r => r.GetChildrenCountAsync(It.IsAny<Guid>())).ReturnsAsync(0);

            var result = await _schoolService.GetActiveSchoolsAsync();

            result.Should().HaveCount(1);
        }

        // Test: Tìm kiếm trường (có filter và phân trang)
        [Fact]
        public async Task SearchSchoolsAsync_FiltersAndPaginates()
        {
            var curriculumId2 = Guid.NewGuid();
            var allSchools = new List<School>
            {
                new School { SchoolId = Guid.NewGuid(), SchoolName = "Test School A", CurriculumId = _curriculumId, IsActive = true },
                new School { SchoolId = Guid.NewGuid(), SchoolName = "Test School B", CurriculumId = curriculumId2, IsActive = true },
                new School { SchoolId = Guid.NewGuid(), SchoolName = "Another Test", CurriculumId = _curriculumId, IsActive = false },
                new School { SchoolId = Guid.NewGuid(), SchoolName = "Test School C", CurriculumId = _curriculumId, IsActive = true }
            };

            _schoolRepositoryMock.Setup(r => r.GetAllAsync()).ReturnsAsync(allSchools);

            _schoolRepositoryMock.Setup(r => r.GetChildrenCountAsync(It.IsAny<Guid>())).ReturnsAsync(0);

            var request = new SchoolSearchRequest
            {
                Name = "Test School",
                CurriculumId = _curriculumId,
                IsActive = true,
                Page = 1,
                PageSize = 1
            };

            var result = await _schoolService.SearchSchoolsAsync(request);

            result.Should().HaveCount(1);
            result.First().SchoolName.Should().Be("Test School A");

            _schoolRepositoryMock.Verify(r => r.GetChildrenCountAsync(It.IsAny<Guid>()), Times.Once);
        }

        // Test: Đếm số lượng trường (có filter)
        [Fact]
        public async Task GetSchoolsCountAsync_FiltersCorrectly()
        {
            var allSchools = new List<School>
            {
                new School { SchoolName = "Test School A", IsActive = true },
                new School { SchoolName = "Test School B", IsActive = true },
                new School { SchoolName = "Another Test", IsActive = false }
            };
            _schoolRepositoryMock.Setup(r => r.GetAllAsync()).ReturnsAsync(allSchools);

            var request = new SchoolSearchRequest { Name = "Test School", IsActive = true };

            var result = await _schoolService.GetSchoolsCountAsync(request);

            result.Should().Be(2); 
        }

        // Test: Lấy trường theo curriculum
        [Fact]
        public async Task GetSchoolsByCurriculumAsync_ReturnsCorrectSchools()
        {
            var schools = new List<School> { new School { SchoolId = Guid.NewGuid(), CurriculumId = _curriculumId } };
            _schoolRepositoryMock.Setup(r => r.GetSchoolsByCurriculumIdAsync(_curriculumId)).ReturnsAsync(schools);
            _schoolRepositoryMock.Setup(r => r.GetChildrenCountAsync(It.IsAny<Guid>())).ReturnsAsync(0);

            var result = await _schoolService.GetSchoolsByCurriculumAsync(_curriculumId);

            result.Should().HaveCount(1);
        }

        // Test: Lấy trường kèm danh sách trẻ em
        [Fact]
        public async Task GetSchoolWithChildrenAsync_Found_ReturnsDtoWithChildren()
        {
            var schoolId = Guid.NewGuid();
            var school = new School { SchoolId = schoolId, Curriculum = _curriculum };
            var children = new List<Child> { new Child { ChildId = Guid.NewGuid(), FullName = "Test Child" } };

            _schoolRepositoryMock.Setup(r => r.GetByIdAsync(schoolId)).ReturnsAsync(school);
            _schoolRepositoryMock.Setup(r => r.GetChildrenBySchoolIdAsync(schoolId)).ReturnsAsync(children);

            var result = await _schoolService.GetSchoolWithChildrenAsync(schoolId);

            result.Should().NotBeNull();
            result.TotalChildren.Should().Be(1);
            result.Children.First().FullName.Should().Be("Test Child");
            result.CurriculumName.Should().Be("Toán 9");
        }

        // Test: Lấy trường kèm danh sách trẻ em (không tìm thấy)
        [Fact]
        public async Task GetSchoolWithChildrenAsync_NotFound_ReturnsNull()
        {
            _schoolRepositoryMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((School)null);
            var result = await _schoolService.GetSchoolWithChildrenAsync(Guid.NewGuid());
            result.Should().BeNull();
        }

        // Test: Lấy danh sách trẻ em theo trường (không tìm thấy trường)
        [Fact]
        public async Task GetChildrenBySchoolAsync_SchoolNotFound_ThrowsKeyNotFoundException()
        {
            _schoolRepositoryMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((School)null);
            Func<Task> act = () => _schoolService.GetChildrenBySchoolAsync(Guid.NewGuid());
            await act.Should().ThrowAsync<KeyNotFoundException>();
        }

        // Test: Lấy danh sách trẻ em theo trường (thành công)
        [Fact]
        public async Task GetChildrenBySchoolAsync_Found_ReturnsChildList()
        {
            var schoolId = Guid.NewGuid();
            var children = new List<Child> { new Child { FullName = "Test Child" } };
            _schoolRepositoryMock.Setup(r => r.GetByIdAsync(schoolId)).ReturnsAsync(new School());
            _schoolRepositoryMock.Setup(r => r.GetChildrenBySchoolIdAsync(schoolId)).ReturnsAsync(children);

            var result = await _schoolService.GetChildrenBySchoolAsync(schoolId);

            result.Should().HaveCount(1);
            result.First().FullName.Should().Be("Test Child");
        }
    }
}