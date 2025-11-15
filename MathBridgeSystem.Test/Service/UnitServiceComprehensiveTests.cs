using FluentAssertions;
using MathBridgeSystem.Application.DTOs;
using MathBridgeSystem.Application.Services;
using MathBridgeSystem.Domain.Entities;
using MathBridgeSystem.Domain.Interfaces;
using Moq;
using Xunit;

namespace MathBridgeSystem.Tests.Services
{
    public class UnitServiceComprehensiveTests
    {
        private readonly Mock<IUnitRepository> _unitRepositoryMock;
        private readonly Mock<ICurriculumRepository> _curriculumRepositoryMock;
        private readonly UnitService _unitService;

        public UnitServiceComprehensiveTests()
        {
            _unitRepositoryMock = new Mock<IUnitRepository>();
            _curriculumRepositoryMock = new Mock<ICurriculumRepository>();
            _unitService = new UnitService(_unitRepositoryMock.Object, _curriculumRepositoryMock.Object);
        }

        #region Constructor Tests

        [Fact]
        public void Constructor_NullUnitRepository_ThrowsArgumentNullException()
        {
            // Act & Assert
            Action act = () => new UnitService(null!, _curriculumRepositoryMock.Object);
            act.Should().Throw<ArgumentNullException>();
        }

        [Fact]
        public void Constructor_NullCurriculumRepository_ThrowsArgumentNullException()
        {
            // Act & Assert
            Action act = () => new UnitService(_unitRepositoryMock.Object, null!);
            act.Should().Throw<ArgumentNullException>();
        }

        #endregion

        #region CreateUnitAsync Tests

        [Fact]
        public async Task CreateUnitAsync_ValidRequest_CreatesUnitSuccessfully()
        {
            // Arrange
            var curriculumId = Guid.NewGuid();
            var request = new CreateUnitRequest
            {
                CurriculumId = curriculumId,
                UnitName = "Unit 1",
                UnitDescription = "Test Description",
                UnitOrder = 1,
                Credit = 3,
                LearningObjectives = "Objectives",
                IsActive = true
            };

            _curriculumRepositoryMock.Setup(r => r.ExistsAsync(curriculumId)).ReturnsAsync(true);
            _unitRepositoryMock.Setup(r => r.ExistsByNameAsync(request.UnitName, curriculumId)).ReturnsAsync(false);
            _unitRepositoryMock.Setup(r => r.AddAsync(It.IsAny<Unit>())).Returns(Task.CompletedTask);

            // Act
            var result = await _unitService.CreateUnitAsync(request);

            // Assert
            result.Should().NotBeEmpty();
            _unitRepositoryMock.Verify(r => r.AddAsync(It.Is<Unit>(u =>
                u.UnitName == request.UnitName &&
                u.CurriculumId == curriculumId
            )), Times.Once);
        }

        [Fact]
        public async Task CreateUnitAsync_EmptyUnitName_ThrowsArgumentException()
        {
            // Arrange
            var request = new CreateUnitRequest
            {
                CurriculumId = Guid.NewGuid(),
                UnitName = "",
                UnitOrder = 1
            };

            // Act
            Func<Task> act = async () => await _unitService.CreateUnitAsync(request);

            // Assert
            await act.Should().ThrowAsync<ArgumentException>()
                .WithMessage("Unit name is required.");
        }

        [Fact]
        public async Task CreateUnitAsync_CurriculumNotFound_ThrowsInvalidOperationException()
        {
            // Arrange
            var curriculumId = Guid.NewGuid();
            var request = new CreateUnitRequest
            {
                CurriculumId = curriculumId,
                UnitName = "Test Unit",
                UnitOrder = 1
            };

            _curriculumRepositoryMock.Setup(r => r.ExistsAsync(curriculumId)).ReturnsAsync(false);

            // Act
            Func<Task> act = async () => await _unitService.CreateUnitAsync(request);

            // Assert
            await act.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("Curriculum not found.");
        }

        [Fact]
        public async Task CreateUnitAsync_DuplicateUnitName_ThrowsInvalidOperationException()
        {
            // Arrange
            var curriculumId = Guid.NewGuid();
            var request = new CreateUnitRequest
            {
                CurriculumId = curriculumId,
                UnitName = "Existing Unit",
                UnitOrder = 1
            };

            _curriculumRepositoryMock.Setup(r => r.ExistsAsync(curriculumId)).ReturnsAsync(true);
            _unitRepositoryMock.Setup(r => r.ExistsByNameAsync(request.UnitName, curriculumId)).ReturnsAsync(true);

            // Act
            Func<Task> act = async () => await _unitService.CreateUnitAsync(request);

            // Assert
            await act.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("*already exists*");
        }

        [Fact]
        public async Task CreateUnitAsync_NoUnitOrderProvided_AutoAssignsOrder()
        {
            // Arrange
            var curriculumId = Guid.NewGuid();
            var request = new CreateUnitRequest
            {
                CurriculumId = curriculumId,
                UnitName = "Unit Auto",
                UnitOrder = null
            };

            _curriculumRepositoryMock.Setup(r => r.ExistsAsync(curriculumId)).ReturnsAsync(true);
            _unitRepositoryMock.Setup(r => r.ExistsByNameAsync(request.UnitName, curriculumId)).ReturnsAsync(false);
            _unitRepositoryMock.Setup(r => r.GetMaxUnitOrderAsync(curriculumId)).ReturnsAsync(5);
            _unitRepositoryMock.Setup(r => r.AddAsync(It.IsAny<Unit>())).Returns(Task.CompletedTask);

            // Act
            await _unitService.CreateUnitAsync(request);

            // Assert
            _unitRepositoryMock.Verify(r => r.AddAsync(It.Is<Unit>(u => u.UnitOrder == 6)), Times.Once);
        }

        #endregion

        #region UpdateUnitAsync Tests

        [Fact]
        public async Task UpdateUnitAsync_ValidRequest_UpdatesUnitSuccessfully()
        {
            // Arrange
            var unitId = Guid.NewGuid();
            var curriculumId = Guid.NewGuid();
            var request = new UpdateUnitRequest
            {
                UnitName = "Updated Unit",
                UnitDescription = "Updated Description",
                UnitOrder = 2,
                Credit = 5,
                IsActive = true
            };

            var existingUnit = new Unit
            {
                UnitId = unitId,
                CurriculumId = curriculumId,
                UnitName = "Old Unit",
                UnitOrder = 1
            };

            _unitRepositoryMock.Setup(r => r.GetByIdAsync(unitId)).ReturnsAsync(existingUnit);
            _unitRepositoryMock.Setup(r => r.GetByNameAsync(request.UnitName)).ReturnsAsync((Unit)null!);
            _unitRepositoryMock.Setup(r => r.UpdateAsync(It.IsAny<Unit>())).Returns(Task.CompletedTask);

            // Act
            await _unitService.UpdateUnitAsync(unitId, request);

            // Assert
            existingUnit.UnitName.Should().Be(request.UnitName);
            existingUnit.UnitOrder.Should().Be(request.UnitOrder);
            _unitRepositoryMock.Verify(r => r.UpdateAsync(existingUnit), Times.Once);
        }

        [Fact]
        public async Task UpdateUnitAsync_EmptyUnitName_ThrowsArgumentException()
        {
            // Arrange
            var request = new UpdateUnitRequest { UnitName = "", UnitOrder = 1 };

            // Act
            Func<Task> act = async () => await _unitService.UpdateUnitAsync(Guid.NewGuid(), request);

            // Assert
            await act.Should().ThrowAsync<ArgumentException>()
                .WithMessage("Unit name is required.");
        }

        [Fact]
        public async Task UpdateUnitAsync_UnitNotFound_ThrowsInvalidOperationException()
        {
            // Arrange
            var request = new UpdateUnitRequest { UnitName = "Test", UnitOrder = 1 };
            _unitRepositoryMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((Unit)null!);

            // Act
            Func<Task> act = async () => await _unitService.UpdateUnitAsync(Guid.NewGuid(), request);

            // Assert
            await act.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("Unit not found.");
        }

        [Fact]
        public async Task UpdateUnitAsync_DuplicateName_ThrowsInvalidOperationException()
        {
            // Arrange
            var unitId = Guid.NewGuid();
            var curriculumId = Guid.NewGuid();
            var request = new UpdateUnitRequest { UnitName = "Duplicate", UnitOrder = 1 };

            var currentUnit = new Unit { UnitId = unitId, CurriculumId = curriculumId, UnitName = "Old Name" };
            var duplicateUnit = new Unit { UnitId = Guid.NewGuid(), CurriculumId = curriculumId, UnitName = "Duplicate" };

            _unitRepositoryMock.Setup(r => r.GetByIdAsync(unitId)).ReturnsAsync(currentUnit);
            _unitRepositoryMock.Setup(r => r.GetByNameAsync(request.UnitName)).ReturnsAsync(duplicateUnit);

            // Act
            Func<Task> act = async () => await _unitService.UpdateUnitAsync(unitId, request);

            // Assert
            await act.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("*already exists*");
        }

        #endregion

        #region DeleteUnitAsync Tests

        [Fact]
        public async Task DeleteUnitAsync_ValidId_DeletesUnitSuccessfully()
        {
            // Arrange
            var unitId = Guid.NewGuid();
            var unit = new Unit { UnitId = unitId, UnitName = "Test Unit" };

            _unitRepositoryMock.Setup(r => r.GetByIdAsync(unitId)).ReturnsAsync(unit);
            _unitRepositoryMock.Setup(r => r.DeleteAsync(unitId)).Returns(Task.CompletedTask);

            // Act
            await _unitService.DeleteUnitAsync(unitId);

            // Assert
            _unitRepositoryMock.Verify(r => r.DeleteAsync(unitId), Times.Once);
        }

        [Fact]
        public async Task DeleteUnitAsync_UnitNotFound_ThrowsInvalidOperationException()
        {
            // Arrange
            _unitRepositoryMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((Unit)null!);

            // Act
            Func<Task> act = async () => await _unitService.DeleteUnitAsync(Guid.NewGuid());

            // Assert
            await act.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("Unit not found.");
        }

        #endregion

        #region GetUnitByIdAsync Tests

        [Fact]
        public async Task GetUnitByIdAsync_ExistingUnit_ReturnsUnitDto()
        {
            // Arrange
            var unitId = Guid.NewGuid();
            var curriculumId = Guid.NewGuid();
            var unit = new Unit
            {
                UnitId = unitId,
                CurriculumId = curriculumId,
                UnitName = "Test Unit",
                UnitDescription = "Test Description",
                UnitOrder = 1,
                Credit = 3,
                IsActive = true,
                Curriculum = new Curriculum { CurriculumName = "Test Curriculum" }
            };

            _unitRepositoryMock.Setup(r => r.GetByIdAsync(unitId)).ReturnsAsync(unit);

            // Act
            var result = await _unitService.GetUnitByIdAsync(unitId);

            // Assert
            result.Should().NotBeNull();
            result!.UnitId.Should().Be(unitId);
            result.UnitName.Should().Be("Test Unit");
            result.CurriculumName.Should().Be("Test Curriculum");
        }

        [Fact]
        public async Task GetUnitByIdAsync_NonExistingUnit_ReturnsNull()
        {
            // Arrange
            _unitRepositoryMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((Unit)null!);

            // Act
            var result = await _unitService.GetUnitByIdAsync(Guid.NewGuid());

            // Assert
            result.Should().BeNull();
        }

        #endregion

        #region GetAllUnitsAsync Tests

        [Fact]
        public async Task GetAllUnitsAsync_ReturnsAllUnits()
        {
            // Arrange
            var units = new List<Unit>
            {
                new Unit { UnitId = Guid.NewGuid(), UnitName = "Unit 1", Curriculum = new Curriculum { CurriculumName = "Curriculum 1" } },
                new Unit { UnitId = Guid.NewGuid(), UnitName = "Unit 2", Curriculum = null }
            };

            _unitRepositoryMock.Setup(r => r.GetAllAsync()).ReturnsAsync(units);

            // Act
            var result = await _unitService.GetAllUnitsAsync();

            // Assert
            result.Should().HaveCount(2);
            result[0].UnitName.Should().Be("Unit 1");
            result[1].CurriculumName.Should().Be(string.Empty);
        }

        [Fact]
        public async Task GetAllUnitsAsync_NoUnits_ReturnsEmptyList()
        {
            // Arrange
            _unitRepositoryMock.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<Unit>());

            // Act
            var result = await _unitService.GetAllUnitsAsync();

            // Assert
            result.Should().BeEmpty();
        }

        #endregion

        #region GetUnitsByCurriculumIdAsync Tests

        [Fact]
        public async Task GetUnitsByCurriculumIdAsync_ReturnsFilteredUnits()
        {
            // Arrange
            var curriculumId = Guid.NewGuid();
            var units = new List<Unit>
            {
                new Unit { UnitId = Guid.NewGuid(), CurriculumId = curriculumId, UnitName = "Unit 1" },
                new Unit { UnitId = Guid.NewGuid(), CurriculumId = curriculumId, UnitName = "Unit 2" }
            };

            _unitRepositoryMock.Setup(r => r.GetByCurriculumIdAsync(curriculumId)).ReturnsAsync(units);

            // Act
            var result = await _unitService.GetUnitsByCurriculumIdAsync(curriculumId);

            // Assert
            result.Should().HaveCount(2);
        }

        #endregion

        #region GetUnitByNameAsync Tests

        [Fact]
        public async Task GetUnitByNameAsync_ExistingUnit_ReturnsUnitDto()
        {
            // Arrange
            var unitName = "Test Unit";
            var unit = new Unit { UnitId = Guid.NewGuid(), UnitName = unitName };

            _unitRepositoryMock.Setup(r => r.GetByNameAsync(unitName)).ReturnsAsync(unit);

            // Act
            var result = await _unitService.GetUnitByNameAsync(unitName);

            // Assert
            result.Should().NotBeNull();
            result!.UnitName.Should().Be(unitName);
        }

        [Fact]
        public async Task GetUnitByNameAsync_NonExistingUnit_ReturnsNull()
        {
            // Arrange
            _unitRepositoryMock.Setup(r => r.GetByNameAsync(It.IsAny<string>())).ReturnsAsync((Unit)null!);

            // Act
            var result = await _unitService.GetUnitByNameAsync("NonExistent");

            // Assert
            result.Should().BeNull();
        }

        #endregion
    }
}
