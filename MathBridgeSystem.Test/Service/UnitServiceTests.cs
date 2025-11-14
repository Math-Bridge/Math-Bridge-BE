using FluentAssertions;
using MathBridgeSystem.Application.DTOs;
using MathBridgeSystem.Application.Services;
using MathBridgeSystem.Domain.Entities;
using MathBridgeSystem.Domain.Interfaces;
using Moq;
using Xunit;

namespace MathBridgeSystem.Tests.Services
{
    public class UnitServiceTests
    {
        private readonly Mock<IUnitRepository> _unitRepositoryMock;
        private readonly Mock<ICurriculumRepository> _curriculumRepositoryMock;
        private readonly UnitService _service;

        public UnitServiceTests()
        {
            _unitRepositoryMock = new Mock<IUnitRepository>();
            _curriculumRepositoryMock = new Mock<ICurriculumRepository>();
            _service = new UnitService(_unitRepositoryMock.Object, _curriculumRepositoryMock.Object);
        }

        [Fact]
        public async Task CreateUnitAsync_ShouldCreateUnit_WhenValid()
        {
            // Arrange
            var curriculumId = Guid.NewGuid();
            var request = new CreateUnitRequest
            {
                CurriculumId = curriculumId,
                UnitName = "Algebra Basics",
                UnitDescription = "Introduction to algebra",
                Credit = 3,
                IsActive = true
            };

            _curriculumRepositoryMock.Setup(r => r.ExistsAsync(curriculumId))
                .ReturnsAsync(true);
            _unitRepositoryMock.Setup(r => r.ExistsByNameAsync(request.UnitName, curriculumId))
                .ReturnsAsync(false);
            _unitRepositoryMock.Setup(r => r.GetMaxUnitOrderAsync(curriculumId))
                .ReturnsAsync(0);
            _unitRepositoryMock.Setup(r => r.AddAsync(It.IsAny<Unit>()))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _service.CreateUnitAsync(request);

            // Assert
            result.Should().NotBeEmpty();
            _unitRepositoryMock.Verify(r => r.AddAsync(It.Is<Unit>(
                u => u.UnitName == "Algebra Basics" && u.UnitOrder == 1
            )), Times.Once);
        }

        [Fact]
        public async Task CreateUnitAsync_ShouldThrowException_WhenUnitNameEmpty()
        {
            // Arrange
            var request = new CreateUnitRequest
            {
                CurriculumId = Guid.NewGuid(),
                UnitName = "",
                IsActive = true
            };

            // Act & Assert
            await Xunit.Assert.ThrowsAsync<ArgumentException>(
                async () => await _service.CreateUnitAsync(request)
            );
        }

        [Fact]
        public async Task CreateUnitAsync_ShouldThrowException_WhenCurriculumNotExists()
        {
            // Arrange
            var curriculumId = Guid.NewGuid();
            var request = new CreateUnitRequest
            {
                CurriculumId = curriculumId,
                UnitName = "Test Unit",
                IsActive = true
            };

            _curriculumRepositoryMock.Setup(r => r.ExistsAsync(curriculumId))
                .ReturnsAsync(false);

            // Act & Assert
            await Xunit.Assert.ThrowsAsync<InvalidOperationException>(
                async () => await _service.CreateUnitAsync(request)
            );
        }

        [Fact]
        public async Task CreateUnitAsync_ShouldThrowException_WhenUnitNameAlreadyExists()
        {
            // Arrange
            var curriculumId = Guid.NewGuid();
            var request = new CreateUnitRequest
            {
                CurriculumId = curriculumId,
                UnitName = "Duplicate Unit",
                IsActive = true
            };

            _curriculumRepositoryMock.Setup(r => r.ExistsAsync(curriculumId))
                .ReturnsAsync(true);
            _unitRepositoryMock.Setup(r => r.ExistsByNameAsync(request.UnitName, curriculumId))
                .ReturnsAsync(true);

            // Act & Assert
            await Xunit.Assert.ThrowsAsync<InvalidOperationException>(
                async () => await _service.CreateUnitAsync(request)
            );
        }

        [Fact]
        public async Task UpdateUnitAsync_ShouldUpdateUnit_WhenValid()
        {
            // Arrange
            var unitId = Guid.NewGuid();
            var curriculumId = Guid.NewGuid();
            var unit = new Unit
            {
                UnitId = unitId,
                CurriculumId = curriculumId,
                UnitName = "Old Name",
                UnitOrder = 1
            };

            var request = new UpdateUnitRequest
            {
                UnitName = "New Name",
                UnitDescription = "Updated description",
                UnitOrder = 2,
                Credit = 4,
                IsActive = true
            };

            _unitRepositoryMock.Setup(r => r.GetByIdAsync(unitId))
                .ReturnsAsync(unit);
            _unitRepositoryMock.Setup(r => r.GetByNameAsync(request.UnitName))
                .ReturnsAsync((Unit)null!);

            // Act
            await _service.UpdateUnitAsync(unitId, request);

            // Assert
            unit.UnitName.Should().Be("New Name");
            unit.UnitOrder.Should().Be(2);
            unit.Credit.Should().Be(4);
            _unitRepositoryMock.Verify(r => r.UpdateAsync(unit), Times.Once);
        }

        [Fact]
        public async Task UpdateUnitAsync_ShouldThrowException_WhenUnitNotFound()
        {
            // Arrange
            var unitId = Guid.NewGuid();
            var request = new UpdateUnitRequest
            {
                UnitName = "Test",
                UnitOrder = 1,
                IsActive = true
            };

            _unitRepositoryMock.Setup(r => r.GetByIdAsync(unitId))
                .ReturnsAsync((Unit)null!);

            // Act & Assert
            await Xunit.Assert.ThrowsAsync<InvalidOperationException>(
                async () => await _service.UpdateUnitAsync(unitId, request)
            );
        }

        [Fact]
        public void Constructor_ShouldThrowArgumentNullException_WhenUnitRepositoryIsNull()
        {
            // Act & Assert
            var action = () => new UnitService(null!, _curriculumRepositoryMock.Object);
            action.Should().Throw<ArgumentNullException>();
        }
    }
}


