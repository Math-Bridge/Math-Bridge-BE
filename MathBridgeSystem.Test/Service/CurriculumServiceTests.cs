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

        [Fact]
        public async Task CreateCurriculumAsync_ValidRequest_ReturnsCurriculumId()
        {
            // Arrange
            var request = new CreateCurriculumRequest { CurriculumCode = "CODE101", CurriculumName = "Test Curriculum", Grades = "1-5" };
            _curriculumRepositoryMock.Setup(repo => repo.ExistsByCodeAsync(request.CurriculumCode)).ReturnsAsync(false);
            _curriculumRepositoryMock.Setup(repo => repo.AddAsync(It.IsAny<Curriculum>())).Returns(Task.CompletedTask);

            // Act
            var result = await _curriculumService.CreateCurriculumAsync(request);

            // Assert
            result.Should().NotBe(Guid.Empty);
            _curriculumRepositoryMock.Verify(repo => repo.AddAsync(It.IsAny<Curriculum>()), Times.Once);
        }

        [Fact]
        public async Task CreateCurriculumAsync_DuplicateCode_ThrowsArgumentException()
        {
            // Arrange
            var request = new CreateCurriculumRequest { CurriculumCode = "DUPLICATE", CurriculumName = "Test Name", Grades = "1-5" }; // Thêm CurriculumName và Grades
            _curriculumRepositoryMock.Setup(repo => repo.ExistsByCodeAsync(request.CurriculumCode)).ReturnsAsync(true);

            // Act & Assert
            Func<Task> act = () => _curriculumService.CreateCurriculumAsync(request);
            await act.Should().ThrowAsync<ArgumentException>().WithMessage("*already exists");
        }

        [Fact]
        public async Task UpdateCurriculumAsync_ValidRequest_UpdatesCurriculum()
        {
            // Arrange
            var id = Guid.NewGuid();
            var request = new UpdateCurriculumRequest { CurriculumName = "Updated Name" };
            var curriculum = new Curriculum { CurriculumId = id, CurriculumName = "Old Name" };
            _curriculumRepositoryMock.Setup(repo => repo.GetByIdAsync(id)).ReturnsAsync(curriculum);
            _curriculumRepositoryMock.Setup(repo => repo.ExistsByCodeAsync(It.IsAny<string>())).ReturnsAsync(false);
            _curriculumRepositoryMock.Setup(repo => repo.UpdateAsync(It.IsAny<Curriculum>())).Returns(Task.CompletedTask);

            // Act
            await _curriculumService.UpdateCurriculumAsync(id, request);

            // Assert
            _curriculumRepositoryMock.Verify(repo => repo.UpdateAsync(It.Is<Curriculum>(c => c.CurriculumName == "Updated Name")), Times.Once);
        }

        [Fact]
        public async Task UpdateCurriculumAsync_NonExisting_ThrowsKeyNotFoundException()
        {
            // Arrange
            var id = Guid.NewGuid();
            var request = new UpdateCurriculumRequest();
            _curriculumRepositoryMock.Setup(repo => repo.GetByIdAsync(id)).ReturnsAsync((Curriculum)null);

            // Act & Assert
            Func<Task> act = () => _curriculumService.UpdateCurriculumAsync(id, request);
            await act.Should().ThrowAsync<KeyNotFoundException>().WithMessage("*not found");
        }

        [Fact]
        public async Task DeleteCurriculumAsync_NoDependencies_DeletesCurriculum()
        {
            // Arrange
            var id = Guid.NewGuid();
            var curriculum = new Curriculum { CurriculumId = id };
            _curriculumRepositoryMock.Setup(repo => repo.GetByIdAsync(id)).ReturnsAsync(curriculum);
            _curriculumRepositoryMock.Setup(repo => repo.GetSchoolsCountAsync(id)).ReturnsAsync(0);
            _curriculumRepositoryMock.Setup(repo => repo.GetPackagesCountAsync(id)).ReturnsAsync(0);
            _curriculumRepositoryMock.Setup(repo => repo.DeleteAsync(id)).Returns(Task.CompletedTask);

            // Act
            await _curriculumService.DeleteCurriculumAsync(id);

            // Assert
            _curriculumRepositoryMock.Verify(repo => repo.DeleteAsync(id), Times.Once);
        }

        [Fact]
        public async Task DeleteCurriculumAsync_HasSchools_ThrowsInvalidOperationException()
        {
            // Arrange
            var id = Guid.NewGuid();
            var curriculum = new Curriculum { CurriculumId = id };
            _curriculumRepositoryMock.Setup(repo => repo.GetByIdAsync(id)).ReturnsAsync(curriculum);
            _curriculumRepositoryMock.Setup(repo => repo.GetSchoolsCountAsync(id)).ReturnsAsync(1);

            // Act & Assert
            Func<Task> act = () => _curriculumService.DeleteCurriculumAsync(id);
            await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*associated schools");
        }

        [Fact]
        public async Task ActivateCurriculumAsync_Inactive_Activates()
        {
            // Arrange
            var id = Guid.NewGuid();
            var curriculum = new Curriculum { IsActive = false };
            _curriculumRepositoryMock.Setup(repo => repo.GetByIdAsync(id)).ReturnsAsync(curriculum);
            _curriculumRepositoryMock.Setup(repo => repo.UpdateAsync(It.IsAny<Curriculum>())).Returns(Task.CompletedTask);

            // Act
            await _curriculumService.ActivateCurriculumAsync(id);

            // Assert
            curriculum.IsActive.Should().BeTrue();
            _curriculumRepositoryMock.Verify(repo => repo.UpdateAsync(It.IsAny<Curriculum>()), Times.Once);
        }

        [Fact]
        public async Task DeactivateCurriculumAsync_Active_Deactivates()
        {
            // Arrange
            var id = Guid.NewGuid();
            var curriculum = new Curriculum { IsActive = true };
            _curriculumRepositoryMock.Setup(repo => repo.GetByIdAsync(id)).ReturnsAsync(curriculum);
            _curriculumRepositoryMock.Setup(repo => repo.UpdateAsync(It.IsAny<Curriculum>())).Returns(Task.CompletedTask);

            // Act
            await _curriculumService.DeactivateCurriculumAsync(id);

            // Assert
            curriculum.IsActive.Should().BeFalse();
            _curriculumRepositoryMock.Verify(repo => repo.UpdateAsync(It.IsAny<Curriculum>()), Times.Once);
        }

        [Fact]
        public async Task GetCurriculumByIdAsync_Existing_ReturnsDto()
        {
            // Arrange
            var id = Guid.NewGuid();
            var curriculum = new Curriculum { CurriculumId = id, CurriculumCode = "CODE101" };
            _curriculumRepositoryMock.Setup(repo => repo.GetByIdAsync(id)).ReturnsAsync(curriculum);
            _curriculumRepositoryMock.Setup(repo => repo.GetSchoolsCountAsync(id)).ReturnsAsync(2);
            _curriculumRepositoryMock.Setup(repo => repo.GetPackagesCountAsync(id)).ReturnsAsync(3);

            // Act
            var result = await _curriculumService.GetCurriculumByIdAsync(id);

            // Assert
            result.CurriculumId.Should().Be(id);
            result.TotalSchools.Should().Be(2);
            result.TotalPackages.Should().Be(3);
        }

        [Fact]
        public async Task GetAllCurriculaAsync_ReturnsList()
        {
            // Arrange
            var curricula = new List<Curriculum> { new Curriculum { CurriculumId = Guid.NewGuid() } };
            _curriculumRepositoryMock.Setup(repo => repo.GetAllAsync()).ReturnsAsync(curricula);
            _curriculumRepositoryMock.Setup(repo => repo.GetSchoolsCountAsync(It.IsAny<Guid>())).ReturnsAsync(0);
            _curriculumRepositoryMock.Setup(repo => repo.GetPackagesCountAsync(It.IsAny<Guid>())).ReturnsAsync(0);

            // Act
            var result = await _curriculumService.GetAllCurriculaAsync();

            // Assert
            result.Should().HaveCount(1);
        }

        [Fact]
        public async Task GetActiveCurriculaAsync_ReturnsActiveList()
        {
            // Arrange
            var curricula = new List<Curriculum> { new Curriculum { IsActive = true } };
            _curriculumRepositoryMock.Setup(repo => repo.GetActiveAsync()).ReturnsAsync(curricula);
            _curriculumRepositoryMock.Setup(repo => repo.GetSchoolsCountAsync(It.IsAny<Guid>())).ReturnsAsync(0);
            _curriculumRepositoryMock.Setup(repo => repo.GetPackagesCountAsync(It.IsAny<Guid>())).ReturnsAsync(0);

            // Act
            var result = await _curriculumService.GetActiveCurriculaAsync();

            // Assert
            result.Should().HaveCount(1);
        }

        [Fact]
        public async Task SearchCurriculaAsync_WithFilters_ReturnsFilteredList()
        {
            // Arrange
            var request = new CurriculumSearchRequest { Name = "Test", Page = 1, PageSize = 10 };
            var curricula = new List<Curriculum> { new Curriculum { CurriculumName = "Test Curriculum" }, new Curriculum { CurriculumName = "Other" } };
            _curriculumRepositoryMock.Setup(repo => repo.GetAllAsync()).ReturnsAsync(curricula);
            _curriculumRepositoryMock.Setup(repo => repo.GetSchoolsCountAsync(It.IsAny<Guid>())).ReturnsAsync(0);
            _curriculumRepositoryMock.Setup(repo => repo.GetPackagesCountAsync(It.IsAny<Guid>())).ReturnsAsync(0);

            // Act
            var result = await _curriculumService.SearchCurriculaAsync(request);

            // Assert
            result.Should().HaveCount(1);
            result[0].CurriculumName.Should().Be("Test Curriculum");
        }

        [Fact]
        public async Task GetCurriculaCountAsync_ReturnsCount()
        {
            // Arrange
            var request = new CurriculumSearchRequest { Name = "Test" };
            var curricula = new List<Curriculum> { new Curriculum { CurriculumName = "Test" } };
            _curriculumRepositoryMock.Setup(repo => repo.GetAllAsync()).ReturnsAsync(curricula);

            // Act
            var result = await _curriculumService.GetCurriculaCountAsync(request);

            // Assert
            result.Should().Be(1);
        }

        [Fact]
        public async Task GetCurriculumWithSchoolsAsync_Existing_ReturnsDtoWithSchools()
        {
            // Arrange
            var id = Guid.NewGuid();
            var curriculum = new Curriculum { CurriculumId = id };
            var schools = new List<School> { new School { Curriculum = curriculum } };
            _curriculumRepositoryMock.Setup(repo => repo.GetByIdAsync(id)).ReturnsAsync(curriculum);
            _curriculumRepositoryMock.Setup(repo => repo.GetSchoolsByCurriculumIdAsync(id)).ReturnsAsync(schools);
            _curriculumRepositoryMock.Setup(repo => repo.GetPackagesCountAsync(id)).ReturnsAsync(0);

            // Act
            var result = await _curriculumService.GetCurriculumWithSchoolsAsync(id);

            // Assert
            result.Should().NotBeNull();
            result.Schools.Should().HaveCount(1);
        }

        [Fact]
        public async Task GetSchoolsByCurriculumAsync_ReturnsList()
        {
            // Arrange
            var id = Guid.NewGuid();
            var schools = new List<School> { new School() };
            _curriculumRepositoryMock.Setup(repo => repo.GetSchoolsByCurriculumIdAsync(id)).ReturnsAsync(schools);

            // Act
            var result = await _curriculumService.GetSchoolsByCurriculumAsync(id);

            // Assert
            result.Should().HaveCount(1);
        }
    }
}