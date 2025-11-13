using FluentAssertions;
using MathBridgeSystem.Api.Controllers;
using MathBridgeSystem.Application.DTOs;
using MathBridgeSystem.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace MathBridgeSystem.Tests.Controllers
{
    public class PackageControllerTests
    {
        private readonly Mock<IPackageService> _packageServiceMock;
        private readonly PackageController _controller;

        public PackageControllerTests()
        {
            _packageServiceMock = new Mock<IPackageService>();
            _controller = new PackageController(_packageServiceMock.Object);
        }

        #region Constructor Tests

        [Fact]
        public void Constructor_NullPackageService_ThrowsArgumentNullException()
        {
            Action act = () => new PackageController(null!);
            act.Should().Throw<ArgumentNullException>().WithParameterName("packageService");
        }

        #endregion

        #region GetAllPackages Tests

        [Fact]
        public async Task GetAllPackages_ReturnsOkWithPackages()
        {
            // Arrange
            var expectedPackages = new List<PaymentPackageDto>
            {
                new PaymentPackageDto { PackageId = Guid.NewGuid(), PackageName = "Basic Package", Price = 100 },
                new PaymentPackageDto { PackageId = Guid.NewGuid(), PackageName = "Premium Package", Price = 200 }
            };
            _packageServiceMock.Setup(s => s.GetAllPackagesAsync())
                .ReturnsAsync(expectedPackages);

            // Act
            var result = await _controller.GetAllPackages();

            // Assert
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            var packages = okResult.Value.Should().BeAssignableTo<List<PaymentPackageDto>>().Subject;
            packages.Should().HaveCount(2);
            _packageServiceMock.Verify(s => s.GetAllPackagesAsync(), Times.Once);
        }

        [Fact]
        public async Task GetAllPackages_EmptyList_ReturnsOkWithEmptyList()
        {
            // Arrange
            _packageServiceMock.Setup(s => s.GetAllPackagesAsync())
                .ReturnsAsync(new List<PaymentPackageDto>());

            // Act
            var result = await _controller.GetAllPackages();

            // Assert
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            var packages = okResult.Value.Should().BeAssignableTo<List<PaymentPackageDto>>().Subject;
            packages.Should().BeEmpty();
        }

        [Fact]
        public async Task GetAllPackages_ServiceThrowsException_ReturnsInternalServerError()
        {
            // Arrange
            _packageServiceMock.Setup(s => s.GetAllPackagesAsync())
                .ThrowsAsync(new Exception("Database error"));

            // Act
            var result = await _controller.GetAllPackages();

            // Assert
            var statusResult = result.Should().BeOfType<ObjectResult>().Subject;
            statusResult.StatusCode.Should().Be(500);
        }

        #endregion

        #region GetPackageById Tests

        [Fact]
        public async Task GetPackageById_ExistingPackage_ReturnsOkWithPackage()
        {
            // Arrange
            var packageId = Guid.NewGuid();
            var expectedPackage = new PaymentPackageDto
            {
                PackageId = packageId,
                PackageName = "Test Package",
                Price = 150
            };
            _packageServiceMock.Setup(s => s.GetPackageByIdAsync(packageId))
                .ReturnsAsync(expectedPackage);

            // Act
            var result = await _controller.GetPackageById(packageId);

            // Assert
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            var package = okResult.Value.Should().BeAssignableTo<PaymentPackageDto>().Subject;
            package.PackageId.Should().Be(packageId);
            _packageServiceMock.Verify(s => s.GetPackageByIdAsync(packageId), Times.Once);
        }

        [Fact]
        public async Task GetPackageById_NonExistingPackage_ReturnsNotFound()
        {
            // Arrange
            var packageId = Guid.NewGuid();
            _packageServiceMock.Setup(s => s.GetPackageByIdAsync(packageId))
                .ReturnsAsync((PaymentPackageDto)null!);

            // Act
            var result = await _controller.GetPackageById(packageId);

            // Assert
            result.Should().BeOfType<NotFoundResult>();
            _packageServiceMock.Verify(s => s.GetPackageByIdAsync(packageId), Times.Once);
        }

        [Fact]
        public async Task GetPackageById_ServiceThrowsException_ReturnsInternalServerError()
        {
            // Arrange
            var packageId = Guid.NewGuid();
            _packageServiceMock.Setup(s => s.GetPackageByIdAsync(packageId))
                .ThrowsAsync(new Exception("Database error"));

            // Act
            var result = await _controller.GetPackageById(packageId);

            // Assert
            var statusResult = result.Should().BeOfType<ObjectResult>().Subject;
            statusResult.StatusCode.Should().Be(500);
        }

        #endregion

        #region GetAllActivePackages Tests

        [Fact]
        public async Task GetAllActivePackages_ReturnsOkWithActivePackages()
        {
            // Arrange
            var expectedPackages = new List<PaymentPackageDto>
            {
                new PaymentPackageDto { PackageId = Guid.NewGuid(), PackageName = "Active Package 1", Price = 100, IsActive = true },
                new PaymentPackageDto { PackageId = Guid.NewGuid(), PackageName = "Active Package 2", Price = 200, IsActive = true }
            };
            _packageServiceMock.Setup(s => s.GetAllActivePackagesAsync())
                .ReturnsAsync(expectedPackages);

            // Act
            var result = await _controller.GetAllActivePackages();

            // Assert
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            var packages = okResult.Value.Should().BeAssignableTo<List<PaymentPackageDto>>().Subject;
            packages.Should().HaveCount(2);
            packages.Should().OnlyContain(p => p.IsActive);
        }

        [Fact]
        public async Task GetAllActivePackages_NoActivePackages_ReturnsOkWithEmptyList()
        {
            // Arrange
            _packageServiceMock.Setup(s => s.GetAllActivePackagesAsync())
                .ReturnsAsync(new List<PaymentPackageDto>());

            // Act
            var result = await _controller.GetAllActivePackages();

            // Assert
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            var packages = okResult.Value.Should().BeAssignableTo<List<PaymentPackageDto>>().Subject;
            packages.Should().BeEmpty();
        }

        [Fact]
        public async Task GetAllActivePackages_ServiceThrowsException_ReturnsInternalServerError()
        {
            // Arrange
            _packageServiceMock.Setup(s => s.GetAllActivePackagesAsync())
                .ThrowsAsync(new Exception("Database error"));

            // Act
            var result = await _controller.GetAllActivePackages();

            // Assert
            var statusResult = result.Should().BeOfType<ObjectResult>().Subject;
            statusResult.StatusCode.Should().Be(500);
        }

        #endregion

        #region GetActivePackageById Tests

        [Fact]
        public async Task GetActivePackageById_ExistingActivePackage_ReturnsOkWithPackage()
        {
            // Arrange
            var packageId = Guid.NewGuid();
            var expectedPackage = new PaymentPackageDto
            {
                PackageId = packageId,
                PackageName = "Active Package",
                Price = 150,
                IsActive = true
            };
            _packageServiceMock.Setup(s => s.GetActivePackageByIdAsync(packageId))
                .ReturnsAsync(expectedPackage);

            // Act
            var result = await _controller.GetActivePackageById(packageId);

            // Assert
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            var package = okResult.Value.Should().BeAssignableTo<PaymentPackageDto>().Subject;
            package.PackageId.Should().Be(packageId);
            package.IsActive.Should().BeTrue();
        }

        [Fact]
        public async Task GetActivePackageById_NonExistingPackage_ReturnsNotFound()
        {
            // Arrange
            var packageId = Guid.NewGuid();
            _packageServiceMock.Setup(s => s.GetActivePackageByIdAsync(packageId))
                .ReturnsAsync((PaymentPackageDto)null!);

            // Act
            var result = await _controller.GetActivePackageById(packageId);

            // Assert
            result.Should().BeOfType<NotFoundResult>();
        }

        [Fact]
        public async Task GetActivePackageById_ServiceThrowsException_ReturnsInternalServerError()
        {
            // Arrange
            var packageId = Guid.NewGuid();
            _packageServiceMock.Setup(s => s.GetActivePackageByIdAsync(packageId))
                .ThrowsAsync(new Exception("Database error"));

            // Act
            var result = await _controller.GetActivePackageById(packageId);

            // Assert
            var statusResult = result.Should().BeOfType<ObjectResult>().Subject;
            statusResult.StatusCode.Should().Be(500);
        }

        #endregion
    }
}

