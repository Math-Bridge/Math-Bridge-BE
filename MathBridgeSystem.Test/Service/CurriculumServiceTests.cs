using FluentAssertions;
using MathBridgeSystem.Application.DTOs.Curriculum;
using MathBridgeSystem.Application.Services;
using MathBridgeSystem.Domain.Entities;
using MathBridgeSystem.Domain.Interfaces;
using Moq;
using Xunit;

namespace MathBridgeSystem.Tests.Services
{
    public class CurriculumServiceTests
    {
        private readonly Mock<ICurriculumRepository> _curriculumRepositoryMock;
        private readonly CurriculumService _curriculumService;

        public CurriculumServiceTests()
        {
            _curriculumRepositoryMock = new Mock<ICurriculumRepository>();
            _curriculumService = new CurriculumService(_curriculumRepositoryMock.Object);
        }

        // Test: Ném lỗi nếu request tạo là null
        [Fact]
        public async Task CreateCurriculumAsync_NullRequest_ThrowsArgumentNullException()
        {
            Func<Task> act = () => _curriculumService.CreateCurriculumAsync(null);
            await act.Should().ThrowAsync<ArgumentNullException>().WithParameterName("request");
        }

        // Test: Ném lỗi nếu thiếu CurriculumCode
        [Fact]
        public async Task CreateCurriculumAsync_MissingCode_ThrowsArgumentException()
        {
            var request = new CreateCurriculumRequest { CurriculumCode = " ", CurriculumName = "Test", Grades = "1-5" };
            Func<Task> act = () => _curriculumService.CreateCurriculumAsync(request);
            await act.Should().ThrowAsync<ArgumentException>().WithParameterName("CurriculumCode");
        }

        // Test: Ném lỗi nếu thiếu CurriculumName
        [Fact]
        public async Task CreateCurriculumAsync_MissingName_ThrowsArgumentException()
        {
            var request = new CreateCurriculumRequest { CurriculumCode = "CODE101", CurriculumName = " ", Grades = "1-5" };
            Func<Task> act = () => _curriculumService.CreateCurriculumAsync(request);
            await act.Should().ThrowAsync<ArgumentException>().WithParameterName("CurriculumName");
        }

        // Test: Ném lỗi nếu thiếu Grades
        [Fact]
        public async Task CreateCurriculumAsync_MissingGrades_ThrowsArgumentException()
        {
            var request = new CreateCurriculumRequest { CurriculumCode = "CODE101", CurriculumName = "Test", Grades = " " };
            Func<Task> act = () => _curriculumService.CreateCurriculumAsync(request);
            await act.Should().ThrowAsync<ArgumentException>().WithParameterName("Grades");
        }

        // Test: Tạo curriculum thành công với request hợp lệ
        [Fact]
        public async Task CreateCurriculumAsync_ValidRequest_ReturnsCurriculumId()
        {
            var request = new CreateCurriculumRequest { CurriculumCode = "CODE101", CurriculumName = "Test Curriculum", Grades = "1-5" };
            _curriculumRepositoryMock.Setup(repo => repo.ExistsByCodeAsync(request.CurriculumCode)).ReturnsAsync(false);
            _curriculumRepositoryMock.Setup(repo => repo.AddAsync(It.IsAny<Curriculum>())).Returns(Task.CompletedTask);

            var result = await _curriculumService.CreateCurriculumAsync(request);

            result.Should().NotBe(Guid.Empty);
            _curriculumRepositoryMock.Verify(repo => repo.AddAsync(It.IsAny<Curriculum>()), Times.Once);
        }

        // Test: Ném lỗi khi tạo curriculum bị trùng code
        [Fact]
        public async Task CreateCurriculumAsync_DuplicateCode_ThrowsArgumentException()
        {
            var request = new CreateCurriculumRequest { CurriculumCode = "DUPLICATE", CurriculumName = "Test Name", Grades = "1-5" };
            _curriculumRepositoryMock.Setup(repo => repo.ExistsByCodeAsync(request.CurriculumCode)).ReturnsAsync(true);

            Func<Task> act = () => _curriculumService.CreateCurriculumAsync(request);
            await act.Should().ThrowAsync<ArgumentException>().WithMessage("*already exists");
        }

        // Test: Ném lỗi nếu request cập nhật là null
        [Fact]
        public async Task UpdateCurriculumAsync_NullRequest_ThrowsArgumentNullException()
        {
            Func<Task> act = () => _curriculumService.UpdateCurriculumAsync(Guid.NewGuid(), null);
            await act.Should().ThrowAsync<ArgumentNullException>().WithParameterName("request");
        }

        // Test: Cập nhật curriculum thành công
        [Fact]
        public async Task UpdateCurriculumAsync_ValidRequest_UpdatesCurriculum()
        {
            var id = Guid.NewGuid();
            var request = new UpdateCurriculumRequest { CurriculumName = "Updated Name" };
            var curriculum = new Curriculum { CurriculumId = id, CurriculumName = "Old Name" };
            _curriculumRepositoryMock.Setup(repo => repo.GetByIdAsync(id)).ReturnsAsync(curriculum);
            _curriculumRepositoryMock.Setup(repo => repo.ExistsByCodeAsync(It.IsAny<string>())).ReturnsAsync(false);
            _curriculumRepositoryMock.Setup(repo => repo.UpdateAsync(It.IsAny<Curriculum>())).Returns(Task.CompletedTask);

            await _curriculumService.UpdateCurriculumAsync(id, request);

            _curriculumRepositoryMock.Verify(repo => repo.UpdateAsync(It.Is<Curriculum>(c => c.CurriculumName == "Updated Name")), Times.Once);
        }

        // Test: Ném lỗi khi cập nhật curriculum với code đã tồn tại
        [Fact]
        public async Task UpdateCurriculumAsync_DuplicateCode_ThrowsArgumentException()
        {
            var id = Guid.NewGuid();
            var request = new UpdateCurriculumRequest { CurriculumCode = "EXISTING_CODE" };
            var curriculum = new Curriculum { CurriculumId = id, CurriculumCode = "OLD_CODE" };

            _curriculumRepositoryMock.Setup(repo => repo.GetByIdAsync(id)).ReturnsAsync(curriculum);
            _curriculumRepositoryMock.Setup(repo => repo.ExistsByCodeAsync("EXISTING_CODE")).ReturnsAsync(true);

            Func<Task> act = () => _curriculumService.UpdateCurriculumAsync(id, request);
            await act.Should().ThrowAsync<ArgumentException>().WithMessage("*already exists");
        }

        // Test: Không gọi UpdateAsync nếu không có gì thay đổi
        [Fact]
        public async Task UpdateCurriculumAsync_NoChanges_DoesNotCallUpdate()
        {
            var id = Guid.NewGuid();
            var request = new UpdateCurriculumRequest { CurriculumName = "Old Name", IsActive = true };
            var curriculum = new Curriculum { CurriculumId = id, CurriculumName = "Old Name", IsActive = true };

            _curriculumRepositoryMock.Setup(repo => repo.GetByIdAsync(id)).ReturnsAsync(curriculum);

            await _curriculumService.UpdateCurriculumAsync(id, request);

            _curriculumRepositoryMock.Verify(repo => repo.UpdateAsync(It.IsAny<Curriculum>()), Times.Never);
        }

        // Test: Ném lỗi khi cập nhật curriculum không tìm thấy
        [Fact]
        public async Task UpdateCurriculumAsync_NonExisting_ThrowsKeyNotFoundException()
        {
            var id = Guid.NewGuid();
            var request = new UpdateCurriculumRequest();
            _curriculumRepositoryMock.Setup(repo => repo.GetByIdAsync(id)).ReturnsAsync((Curriculum)null);

            Func<Task> act = () => _curriculumService.UpdateCurriculumAsync(id, request);
            await act.Should().ThrowAsync<KeyNotFoundException>().WithMessage("*not found");
        }

        // Test: Ném lỗi khi xóa curriculum không tìm thấy
        [Fact]
        public async Task DeleteCurriculumAsync_CurriculumNotFound_ThrowsKeyNotFoundException()
        {
            _curriculumRepositoryMock.Setup(repo => repo.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((Curriculum)null);
            Func<Task> act = () => _curriculumService.DeleteCurriculumAsync(Guid.NewGuid());
            await act.Should().ThrowAsync<KeyNotFoundException>();
        }

        // Test: Xóa curriculum thành công khi không có ràng buộc
        [Fact]
        public async Task DeleteCurriculumAsync_NoDependencies_DeletesCurriculum()
        {
            var id = Guid.NewGuid();
            var curriculum = new Curriculum { CurriculumId = id };
            _curriculumRepositoryMock.Setup(repo => repo.GetByIdAsync(id)).ReturnsAsync(curriculum);
            _curriculumRepositoryMock.Setup(repo => repo.GetSchoolsCountAsync(id)).ReturnsAsync(0);
            _curriculumRepositoryMock.Setup(repo => repo.GetPackagesCountAsync(id)).ReturnsAsync(0);
            _curriculumRepositoryMock.Setup(repo => repo.DeleteAsync(id)).Returns(Task.CompletedTask);

            await _curriculumService.DeleteCurriculumAsync(id);

            _curriculumRepositoryMock.Verify(repo => repo.DeleteAsync(id), Times.Once);
        }

        // Test: Ném lỗi khi xóa curriculum còn liên kết với trường (schools)
        [Fact]
        public async Task DeleteCurriculumAsync_HasSchools_ThrowsInvalidOperationException()
        {
            var id = Guid.NewGuid();
            var curriculum = new Curriculum { CurriculumId = id };
            _curriculumRepositoryMock.Setup(repo => repo.GetByIdAsync(id)).ReturnsAsync(curriculum);
            _curriculumRepositoryMock.Setup(repo => repo.GetSchoolsCountAsync(id)).ReturnsAsync(1); 

            Func<Task> act = () => _curriculumService.DeleteCurriculumAsync(id);
            await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*associated schools");
        }

        // Test: Ném lỗi khi xóa curriculum còn liên kết với gói (packages)
        [Fact]
        public async Task DeleteCurriculumAsync_HasPackages_ThrowsInvalidOperationException()
        {
            var id = Guid.NewGuid();
            var curriculum = new Curriculum { CurriculumId = id };
            _curriculumRepositoryMock.Setup(repo => repo.GetByIdAsync(id)).ReturnsAsync(curriculum);
            _curriculumRepositoryMock.Setup(repo => repo.GetSchoolsCountAsync(id)).ReturnsAsync(0);
            _curriculumRepositoryMock.Setup(repo => repo.GetPackagesCountAsync(id)).ReturnsAsync(1); 

            Func<Task> act = () => _curriculumService.DeleteCurriculumAsync(id);
            await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*associated payment packages");
        }

        // Test: Ném lỗi khi kích hoạt curriculum không tìm thấy
        [Fact]
        public async Task ActivateCurriculumAsync_CurriculumNotFound_ThrowsKeyNotFoundException()
        {
            _curriculumRepositoryMock.Setup(repo => repo.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((Curriculum)null);
            Func<Task> act = () => _curriculumService.ActivateCurriculumAsync(Guid.NewGuid());
            await act.Should().ThrowAsync<KeyNotFoundException>();
        }

        // Test: Kích hoạt curriculum đang bị vô hiệu hóa
        [Fact]
        public async Task ActivateCurriculumAsync_Inactive_Activates()
        {
            var id = Guid.NewGuid();
            var curriculum = new Curriculum { IsActive = false };
            _curriculumRepositoryMock.Setup(repo => repo.GetByIdAsync(id)).ReturnsAsync(curriculum);
            _curriculumRepositoryMock.Setup(repo => repo.UpdateAsync(It.IsAny<Curriculum>())).Returns(Task.CompletedTask);

            await _curriculumService.ActivateCurriculumAsync(id);

            curriculum.IsActive.Should().BeTrue();
            _curriculumRepositoryMock.Verify(repo => repo.UpdateAsync(It.IsAny<Curriculum>()), Times.Once);
        }

        // Test: Không gọi UpdateAsync khi kích hoạt curriculum đã active
        [Fact]
        public async Task ActivateCurriculumAsync_AlreadyActive_DoesNotCallUpdate()
        {
            var id = Guid.NewGuid();
            var curriculum = new Curriculum { IsActive = true }; 
            _curriculumRepositoryMock.Setup(repo => repo.GetByIdAsync(id)).ReturnsAsync(curriculum);

            await _curriculumService.ActivateCurriculumAsync(id);

            _curriculumRepositoryMock.Verify(repo => repo.UpdateAsync(It.IsAny<Curriculum>()), Times.Never);
        }

        // Test: Ném lỗi khi vô hiệu hóa curriculum không tìm thấy
        [Fact]
        public async Task DeactivateCurriculumAsync_CurriculumNotFound_ThrowsKeyNotFoundException()
        {
            _curriculumRepositoryMock.Setup(repo => repo.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((Curriculum)null);
            Func<Task> act = () => _curriculumService.DeactivateCurriculumAsync(Guid.NewGuid());
            await act.Should().ThrowAsync<KeyNotFoundException>();
        }

        // Test: Vô hiệu hóa curriculum đang active
        [Fact]
        public async Task DeactivateCurriculumAsync_Active_Deactivates()
        {
            var id = Guid.NewGuid();
            var curriculum = new Curriculum { IsActive = true };
            _curriculumRepositoryMock.Setup(repo => repo.GetByIdAsync(id)).ReturnsAsync(curriculum);
            _curriculumRepositoryMock.Setup(repo => repo.UpdateAsync(It.IsAny<Curriculum>())).Returns(Task.CompletedTask);

            await _curriculumService.DeactivateCurriculumAsync(id);

            curriculum.IsActive.Should().BeFalse();
            _curriculumRepositoryMock.Verify(repo => repo.UpdateAsync(It.IsAny<Curriculum>()), Times.Once);
        }

        // Test: Không gọi UpdateAsync khi vô hiệu hóa curriculum đã inactive
        [Fact]
        public async Task DeactivateCurriculumAsync_AlreadyInactive_DoesNotCallUpdate()
        {
            var id = Guid.NewGuid();
            var curriculum = new Curriculum { IsActive = false }; 
            _curriculumRepositoryMock.Setup(repo => repo.GetByIdAsync(id)).ReturnsAsync(curriculum);

            await _curriculumService.DeactivateCurriculumAsync(id);

            _curriculumRepositoryMock.Verify(repo => repo.UpdateAsync(It.IsAny<Curriculum>()), Times.Never);
        }

        // Test: Lấy curriculum bằng ID thành công
        [Fact]
        public async Task GetCurriculumByIdAsync_Existing_ReturnsDto()
        {
            var id = Guid.NewGuid();
            var curriculum = new Curriculum { CurriculumId = id, CurriculumCode = "CODE101" };
            _curriculumRepositoryMock.Setup(repo => repo.GetByIdAsync(id)).ReturnsAsync(curriculum);
            _curriculumRepositoryMock.Setup(repo => repo.GetSchoolsCountAsync(id)).ReturnsAsync(2);
            _curriculumRepositoryMock.Setup(repo => repo.GetPackagesCountAsync(id)).ReturnsAsync(3);

            var result = await _curriculumService.GetCurriculumByIdAsync(id);

            result.CurriculumId.Should().Be(id);
            result.TotalSchools.Should().Be(2);
            result.TotalPackages.Should().Be(3);
        }

        // Test: Lấy curriculum bằng ID (không tìm thấy, trả về null)
        [Fact]
        public async Task GetCurriculumByIdAsync_NotFound_ReturnsNull()
        {
            _curriculumRepositoryMock.Setup(repo => repo.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((Curriculum)null);
            var result = await _curriculumService.GetCurriculumByIdAsync(Guid.NewGuid());
            result.Should().BeNull();
        }

        // Test: Lấy tất cả curricula
        [Fact]
        public async Task GetAllCurriculaAsync_ReturnsList()
        {
            var curricula = new List<Curriculum> { new Curriculum { CurriculumId = Guid.NewGuid() } };
            _curriculumRepositoryMock.Setup(repo => repo.GetAllAsync()).ReturnsAsync(curricula);
            _curriculumRepositoryMock.Setup(repo => repo.GetSchoolsCountAsync(It.IsAny<Guid>())).ReturnsAsync(0);
            _curriculumRepositoryMock.Setup(repo => repo.GetPackagesCountAsync(It.IsAny<Guid>())).ReturnsAsync(0);

            var result = await _curriculumService.GetAllCurriculaAsync();

            result.Should().HaveCount(1);
        }

        // Test: Lấy tất cả curricula (danh sách rỗng)
        [Fact]
        public async Task GetAllCurriculaAsync_NoCurricula_ReturnsEmptyList()
        {
            _curriculumRepositoryMock.Setup(repo => repo.GetAllAsync()).ReturnsAsync(new List<Curriculum>());
            var result = await _curriculumService.GetAllCurriculaAsync();
            result.Should().BeEmpty();
        }

        // Test: Lấy các curricula đang active
        [Fact]
        public async Task GetActiveCurriculaAsync_ReturnsActiveList()
        {
            var curricula = new List<Curriculum> { new Curriculum { IsActive = true } };
            _curriculumRepositoryMock.Setup(repo => repo.GetActiveAsync()).ReturnsAsync(curricula);
            _curriculumRepositoryMock.Setup(repo => repo.GetSchoolsCountAsync(It.IsAny<Guid>())).ReturnsAsync(0);
            _curriculumRepositoryMock.Setup(repo => repo.GetPackagesCountAsync(It.IsAny<Guid>())).ReturnsAsync(0);

            var result = await _curriculumService.GetActiveCurriculaAsync();

            result.Should().HaveCount(1);
        }

        // Test: Lấy các curricula đang active (danh sách rỗng)
        [Fact]
        public async Task GetActiveCurriculaAsync_NoActiveCurricula_ReturnsEmptyList()
        {
            _curriculumRepositoryMock.Setup(repo => repo.GetActiveAsync()).ReturnsAsync(new List<Curriculum>());
            var result = await _curriculumService.GetActiveCurriculaAsync();
            result.Should().BeEmpty();
        }

        // Test: Ném lỗi khi tìm kiếm (request là null)
        [Fact]
        public async Task SearchCurriculaAsync_NullRequest_ThrowsArgumentNullException()
        {
            Func<Task> act = () => _curriculumService.SearchCurriculaAsync(null);
            await act.Should().ThrowAsync<ArgumentNullException>().WithParameterName("request");
        }

        // Test: Tìm kiếm curricula theo bộ lọc (Name)
        [Fact]
        public async Task SearchCurriculaAsync_WithFilters_ReturnsFilteredList()
        {
            var request = new CurriculumSearchRequest { Name = "Test", Page = 1, PageSize = 10 };
            var curricula = new List<Curriculum> { new Curriculum { CurriculumName = "Test Curriculum" }, new Curriculum { CurriculumName = "Other" } };
            _curriculumRepositoryMock.Setup(repo => repo.GetAllAsync()).ReturnsAsync(curricula);
            _curriculumRepositoryMock.Setup(repo => repo.GetSchoolsCountAsync(It.IsAny<Guid>())).ReturnsAsync(0);
            _curriculumRepositoryMock.Setup(repo => repo.GetPackagesCountAsync(It.IsAny<Guid>())).ReturnsAsync(0);

            var result = await _curriculumService.SearchCurriculaAsync(request);

            result.Should().HaveCount(1);
            result[0].CurriculumName.Should().Be("Test Curriculum");
        }

        // Test: Tìm kiếm curricula với tất cả bộ lọc và phân trang
        [Fact]
        public async Task SearchCurriculaAsync_AllFiltersAndPagination_ReturnsCorrectList()
        {
            var curricula = new List<Curriculum>
            {
                new Curriculum { CurriculumName = "Math 9", CurriculumCode = "M9", Grades = "9", IsActive = true },
                new Curriculum { CurriculumName = "Math 10", CurriculumCode = "M10", Grades = "10", IsActive = true },
                new Curriculum { CurriculumName = "Math 11", CurriculumCode = "M11", Grades = "11", IsActive = false },
                new Curriculum { CurriculumName = "Physics 10", CurriculumCode = "P10", Grades = "10", IsActive = true },
            };
            _curriculumRepositoryMock.Setup(repo => repo.GetAllAsync()).ReturnsAsync(curricula);

            var request = new CurriculumSearchRequest
            {
                Name = "Math",
                Grades = "10",
                IsActive = true,
                Page = 1,
                PageSize = 10
            };

            var result = await _curriculumService.SearchCurriculaAsync(request);
            result.Should().HaveCount(1);
            result[0].CurriculumCode.Should().Be("M10");
        }

        // Test: Ném lỗi khi đếm (request là null)
        [Fact]
        public async Task GetCurriculaCountAsync_NullRequest_ThrowsArgumentNullException()
        {
            Func<Task> act = () => _curriculumService.GetCurriculaCountAsync(null);
            await act.Should().ThrowAsync<ArgumentNullException>().WithParameterName("request");
        }

        // Test: Đếm số lượng curricula theo bộ lọc
        [Fact]
        public async Task GetCurriculaCountAsync_ReturnsCount()
        {
            var request = new CurriculumSearchRequest { Name = "Test" };
            var curricula = new List<Curriculum> { new Curriculum { CurriculumName = "Test" } };
            _curriculumRepositoryMock.Setup(repo => repo.GetAllAsync()).ReturnsAsync(curricula);

            var result = await _curriculumService.GetCurriculaCountAsync(request);

            result.Should().Be(1);
        }

        // Test: Lấy curriculum và danh sách trường liên kết
        [Fact]
        public async Task GetCurriculumWithSchoolsAsync_Existing_ReturnsDtoWithSchools()
        {
            var id = Guid.NewGuid();
            var curriculum = new Curriculum { CurriculumId = id };
            var schools = new List<School> { new School { Curriculum = curriculum } };
            _curriculumRepositoryMock.Setup(repo => repo.GetByIdAsync(id)).ReturnsAsync(curriculum);
            _curriculumRepositoryMock.Setup(repo => repo.GetSchoolsByCurriculumIdAsync(id)).ReturnsAsync(schools);
            _curriculumRepositoryMock.Setup(repo => repo.GetPackagesCountAsync(id)).ReturnsAsync(0);

            var result = await _curriculumService.GetCurriculumWithSchoolsAsync(id);

            result.Should().NotBeNull();
            result.Schools.Should().HaveCount(1);
        }

        // Test: Lấy curriculum và trường (không tìm thấy, trả về null)
        [Fact]
        public async Task GetCurriculumWithSchoolsAsync_NotFound_ReturnsNull()
        {
            _curriculumRepositoryMock.Setup(repo => repo.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((Curriculum)null);
            var result = await _curriculumService.GetCurriculumWithSchoolsAsync(Guid.NewGuid());
            result.Should().BeNull();
        }

        // Test: Lấy danh sách trường theo ID curriculum
        [Fact]
        public async Task GetSchoolsByCurriculumAsync_ReturnsList()
        {
            var id = Guid.NewGuid();
            var schools = new List<School> { new School() };
            _curriculumRepositoryMock.Setup(repo => repo.GetSchoolsByCurriculumIdAsync(id)).ReturnsAsync(schools);

            var result = await _curriculumService.GetSchoolsByCurriculumAsync(id);

            result.Should().HaveCount(1);
        }

        // Test: Lấy danh sách trường theo ID curriculum (danh sách rỗng)
        [Fact]
        public async Task GetSchoolsByCurriculumAsync_NoSchools_ReturnsEmptyList()
        {
            _curriculumRepositoryMock.Setup(repo => repo.GetSchoolsByCurriculumIdAsync(It.IsAny<Guid>())).ReturnsAsync(new List<School>());
            var result = await _curriculumService.GetSchoolsByCurriculumAsync(Guid.NewGuid());
            result.Should().BeEmpty();
        }
    }
}