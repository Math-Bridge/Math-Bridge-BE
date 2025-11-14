using FluentAssertions;
using MathBridgeSystem.Application.DTOs;
using MathBridgeSystem.Application.Interfaces;
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
    public class PackageServiceTests
    {
        private readonly Mock<IPackageRepository> _packageRepositoryMock;
        private readonly PackageService _packageService;

        public PackageServiceTests()
        {
            _packageRepositoryMock = new Mock<IPackageRepository>();
            _packageService = new PackageService(_packageRepositoryMock.Object);
        }


        private CreatePackageRequest CreateValidCreateRequest()
        {
            return new CreatePackageRequest
            {
                PackageName = "Gói 1",
                Grade = "grade 10",
                Price = 1000,
                CurriculumId = Guid.NewGuid(),
                SessionsPerWeek = 3,
                SessionCount = 12,
                MaxReschedule = 2,
                DurationDays = 90,
                IsActive = true
            };
        }


        // Test: Tạo gói thành công
        [Fact]
        public async Task CreatePackageAsync_ValidRequest_CreatesAndReturnsId()
        {
            // Arrange
            var request = CreateValidCreateRequest();
            _packageRepositoryMock.Setup(r => r.ExistsCurriculumAsync(request.CurriculumId)).ReturnsAsync(true);
            _packageRepositoryMock.Setup(r => r.AddAsync(It.IsAny<PaymentPackage>())).Returns(Task.CompletedTask);

            // Act
            var result = await _packageService.CreatePackageAsync(request);

            // Assert
            result.Should().NotBe(Guid.Empty);
            _packageRepositoryMock.Verify(r => r.AddAsync(It.Is<PaymentPackage>(p => p.PackageName == "Gói 1")), Times.Once);
        }

        // Test: Ném lỗi khi thiếu tên gói
        [Fact]
        public async Task CreatePackageAsync_MissingPackageName_ThrowsArgumentException()
        {
            // Arrange
            var request = CreateValidCreateRequest();
            request.PackageName = " "; 

            // Act
            Func<Task> act = () => _packageService.CreatePackageAsync(request);

            // Assert
            await act.Should().ThrowAsync<ArgumentException>().WithMessage("Package name is required.");
        }

        // Test: Ném lỗi khi khối lớp (grade) không hợp lệ
        [Fact]
        public async Task CreatePackageAsync_InvalidGrade_ThrowsArgumentException()
        {
            // Arrange
            var request = CreateValidCreateRequest();
            request.Grade = "grade 8"; 

            // Act
            Func<Task> act = () => _packageService.CreatePackageAsync(request);

            // Assert
            await act.Should().ThrowAsync<ArgumentException>().WithMessage("Invalid grade. Must be grade 9, 10, 11, or 12.");
        }

        // Test: Ném lỗi khi giá bằng 0
        [Fact]
        public async Task CreatePackageAsync_ZeroPrice_ThrowsArgumentException()
        {
            // Arrange
            var request = CreateValidCreateRequest();
            request.Price = 0; 

            // Act
            Func<Task> act = () => _packageService.CreatePackageAsync(request);

            // Assert
            await act.Should().ThrowAsync<ArgumentException>().WithMessage("Price must be greater than 0.");
        }

        // Test: Ném lỗi khi giá âm
        [Fact]
        public async Task CreatePackageAsync_NegativePrice_ThrowsArgumentException()
        {
            // Arrange
            var request = CreateValidCreateRequest();
            request.Price = -100; 

            // Act
            Func<Task> act = () => _packageService.CreatePackageAsync(request);

            // Assert
            await act.Should().ThrowAsync<ArgumentException>().WithMessage("Price must be greater than 0.");
        }

        // Test: Ném lỗi khi không tìm thấy Curriculum
        [Fact]
        public async Task CreatePackageAsync_CurriculumNotFound_ThrowsKeyNotFoundException()
        {
            // Arrange
            var request = CreateValidCreateRequest();
            _packageRepositoryMock.Setup(r => r.ExistsCurriculumAsync(request.CurriculumId)).ReturnsAsync(false); 

            // Act
            Func<Task> act = () => _packageService.CreatePackageAsync(request);

            // Assert
            await Xunit.Assert.ThrowsAnyAsync<Exception>(async () => await act());
        }


        // Test: Lấy tất cả các gói (khi có dữ liệu)
        [Fact]
        public async Task GetAllPackagesAsync_WhenPackagesExist_ReturnsDtoList()
        {
            // Arrange
            var packages = new List<PaymentPackage>
            {
                new PaymentPackage { PackageId = Guid.NewGuid(), PackageName = "Gói 1" },
                new PaymentPackage { PackageId = Guid.NewGuid(), PackageName = "Gói 2" }
            };
            _packageRepositoryMock.Setup(r => r.GetAllAsync()).ReturnsAsync(packages);

            // Act
            var result = await _packageService.GetAllPackagesAsync();

            // Assert
            result.Should().HaveCount(2);
            result.First().PackageName.Should().Be("Gói 1");
        }

        // Test: Lấy tất cả các gói (khi không có dữ liệu)
        [Fact]
        public async Task GetAllPackagesAsync_WhenNoPackages_ReturnsEmptyList()
        {
            // Arrange
            _packageRepositoryMock.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<PaymentPackage>());

            // Act
            var result = await _packageService.GetAllPackagesAsync();

            // Assert
            result.Should().BeEmpty();
        }


        // Test: Cập nhật gói thành công (cập nhật tất cả các trường)
        [Fact]
        public async Task UpdatePackageAsync_ValidRequest_UpdatesAndReturnsDto()
        {
            // Arrange
            var packageId = Guid.NewGuid();
            var newCurriculumId = Guid.NewGuid();
            var request = new UpdatePackageRequest
            {
                PackageName = "Tên Mới",
                Grade = "grade 11",
                Price = 2000,
                CurriculumId = newCurriculumId,
                Description = "Mô tả mới"
            };

            var existingPackage = new PaymentPackage { PackageId = packageId, PackageName = "Tên Cũ", Grade = "grade 10", Price = 1000 };

            _packageRepositoryMock.Setup(r => r.GetByIdAsync(packageId)).ReturnsAsync(existingPackage);
            _packageRepositoryMock.Setup(r => r.ExistsCurriculumAsync(newCurriculumId)).ReturnsAsync(true);
            _packageRepositoryMock.Setup(r => r.UpdateAsync(It.IsAny<PaymentPackage>())).Returns(Task.CompletedTask);

            // Act
            var result = await _packageService.UpdatePackageAsync(packageId, request);

            // Assert
            result.PackageName.Should().Be("Tên Mới");
            result.Grade.Should().Be("grade 11");
            result.Price.Should().Be(2000);
            result.Description.Should().Be("Mô tả mới");

            existingPackage.PackageName.Should().Be("Tên Mới");
            existingPackage.CurriculumId.Should().Be(newCurriculumId);
            existingPackage.UpdatedDate.Should().NotBeNull();

            _packageRepositoryMock.Verify(r => r.UpdateAsync(existingPackage), Times.Once);
        }

        // Test: Ném lỗi khi cập nhật gói không tìm thấy
        [Fact]
        public async Task UpdatePackageAsync_PackageNotFound_ThrowsKeyNotFoundException()
        {
            // Arrange
            _packageRepositoryMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((PaymentPackage)null);

            // Act
            Func<Task> act = () => _packageService.UpdatePackageAsync(Guid.NewGuid(), new UpdatePackageRequest());

            // Assert
            await act.Should().ThrowAsync<KeyNotFoundException>().WithMessage("Package not found.");
        }

        // Test: Ném lỗi khi cập nhật khối lớp (grade) không hợp lệ
        [Fact]
        public async Task UpdatePackageAsync_InvalidGrade_ThrowsArgumentException()
        {
            // Arrange
            var request = new UpdatePackageRequest { Grade = "grade 8" };
            var existingPackage = new PaymentPackage();
            _packageRepositoryMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync(existingPackage);

            // Act
            Func<Task> act = () => _packageService.UpdatePackageAsync(Guid.NewGuid(), request);

            // Assert
            await act.Should().ThrowAsync<ArgumentException>().WithMessage("Invalid grade.");
        }

        // Test: Ném lỗi khi cập nhật giá bằng 0
        [Fact]
        public async Task UpdatePackageAsync_ZeroPrice_ThrowsArgumentException()
        {
            // Arrange
            var request = new UpdatePackageRequest { Price = 0 };
            var existingPackage = new PaymentPackage();
            _packageRepositoryMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync(existingPackage);

            // Act
            Func<Task> act = () => _packageService.UpdatePackageAsync(Guid.NewGuid(), request);

            // Assert
            await act.Should().ThrowAsync<ArgumentException>().WithMessage("Price must be greater than 0.");
        }

        // Test: Ném lỗi khi cập nhật CurriculumId không tồn tại
        [Fact]
        public async Task UpdatePackageAsync_CurriculumNotFound_ThrowsKeyNotFoundException()
        {
            // Arrange
            var newCurriculumId = Guid.NewGuid();
            var request = new UpdatePackageRequest { CurriculumId = newCurriculumId };
            var existingPackage = new PaymentPackage();

            _packageRepositoryMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync(existingPackage);
            _packageRepositoryMock.Setup(r => r.ExistsCurriculumAsync(newCurriculumId)).ReturnsAsync(false); 

            // Act
            Func<Task> act = () => _packageService.UpdatePackageAsync(Guid.NewGuid(), request);

            // Assert
            await Xunit.Assert.ThrowsAnyAsync<Exception>(async () => await act());
        }

        // Test: Cập nhật gói (không có trường nào thay đổi, vẫn gọi Update)
        [Fact]
        public async Task UpdatePackageAsync_EmptyRequest_StillCallsUpdate()
        {
            // Arrange
            var packageId = Guid.NewGuid();
            var request = new UpdatePackageRequest(); 
            var existingPackage = new PaymentPackage { PackageId = packageId };

            _packageRepositoryMock.Setup(r => r.GetByIdAsync(packageId)).ReturnsAsync(existingPackage);

            // Act
            await _packageService.UpdatePackageAsync(packageId, request);

            // Assert
            _packageRepositoryMock.Verify(r => r.UpdateAsync(It.Is<PaymentPackage>(p => p.UpdatedDate.HasValue)), Times.Once);
        }


        // Test: Xóa gói thành công
        [Fact]
        public async Task DeletePackageAsync_ValidId_DeletesPackage()
        {
            // Arrange
            var packageId = Guid.NewGuid();
            var existingPackage = new PaymentPackage { PackageId = packageId };
            _packageRepositoryMock.Setup(r => r.GetByIdAsync(packageId)).ReturnsAsync(existingPackage);
            _packageRepositoryMock.Setup(r => r.IsPackageInUseAsync(packageId)).ReturnsAsync(false); 
            _packageRepositoryMock.Setup(r => r.DeleteAsync(packageId)).Returns(Task.CompletedTask);

            // Act
            await _packageService.DeletePackageAsync(packageId);

            // Assert
            _packageRepositoryMock.Verify(r => r.DeleteAsync(packageId), Times.Once);
        }

        // Test: Ném lỗi khi xóa gói không tìm thấy
        [Fact]
        public async Task DeletePackageAsync_PackageNotFound_ThrowsKeyNotFoundException()
        {
            // Arrange
            var packageId = Guid.NewGuid();
            _packageRepositoryMock.Setup(r => r.GetByIdAsync(packageId)).ReturnsAsync((PaymentPackage)null);

            // Act
            Func<Task> act = () => _packageService.DeletePackageAsync(packageId);

            // Assert
            await act.Should().ThrowAsync<KeyNotFoundException>().WithMessage("Package not found.");
        }

        // Test: Ném lỗi khi xóa gói đang được sử dụng
        [Fact]
        public async Task DeletePackageAsync_PackageInUse_ThrowsInvalidOperationException()
        {
            // Arrange
            var packageId = Guid.NewGuid();
            var existingPackage = new PaymentPackage { PackageId = packageId };
            _packageRepositoryMock.Setup(r => r.GetByIdAsync(packageId)).ReturnsAsync(existingPackage);
            _packageRepositoryMock.Setup(r => r.IsPackageInUseAsync(packageId)).ReturnsAsync(true); 

            // Act
            Func<Task> act = () => _packageService.DeletePackageAsync(packageId);

            // Assert
            await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("Cannot delete package because it is used in one or more contracts.");
            _packageRepositoryMock.Verify(r => r.DeleteAsync(packageId), Times.Never); 
        }
    }
}