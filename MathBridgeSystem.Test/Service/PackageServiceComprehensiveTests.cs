using FluentAssertions;
using MathBridgeSystem.Application.DTOs;
using MathBridgeSystem.Application.Services;
using MathBridgeSystem.Application.Interfaces;
using MathBridgeSystem.Domain.Entities;
using MathBridgeSystem.Domain.Interfaces;
using Moq;
using Xunit;

namespace MathBridgeSystem.Tests.Services
{
    public class PackageServiceComprehensiveTests
    {
        private readonly Mock<IPackageRepository> _repo;
        private readonly Mock<ICloudinaryService> _cloudinaryServiceMock;
        private readonly PackageService _service;

        public PackageServiceComprehensiveTests()
        {
            _repo = new Mock<IPackageRepository>();
            _cloudinaryServiceMock = new Mock<ICloudinaryService>();
            _service = new PackageService(_repo.Object, _cloudinaryServiceMock.Object);
        }

        [Fact]
        public void Ctor_NullRepo_Throws()
        {
            Action act = () => new PackageService(null!, _cloudinaryServiceMock.Object);
            act.Should().Throw<ArgumentNullException>();
        }

        [Fact]
        public async Task CreatePackageAsync_Valid_Creates()
        {
            var req = new CreatePackageRequest
            {
                PackageName = "Math Pro",
                Grade = "grade 10",
                Price = 200,
                SessionCount = 36,
                SessionsPerWeek = 3,
                MaxReschedule = 5,
                DurationDays = 90,
                Description = "Desc",
                CurriculumId = Guid.NewGuid(),
                IsActive = true
            };
            _repo.Setup(r => r.ExistsCurriculumAsync(req.CurriculumId)).ReturnsAsync(true);
            _repo.Setup(r => r.AddAsync(It.IsAny<PaymentPackage>())).Returns(Task.CompletedTask);

            var id = await _service.CreatePackageAsync(req);
            id.Should().NotBeEmpty();
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public async Task CreatePackageAsync_InvalidName_Throws(string? name)
        {
            var req = new CreatePackageRequest { PackageName = name ?? "", Grade = "grade 10", Price = 100, SessionsPerWeek = 3, CurriculumId = Guid.NewGuid() };
            await FluentActions.Invoking(() => _service.CreatePackageAsync(req))
                .Should().ThrowAsync<ArgumentException>();
        }

        [Fact]
        public async Task CreatePackageAsync_InvalidGrade_Throws()
        {
            var req = new CreatePackageRequest { PackageName = "X", Grade = "grade 8", Price = 100, SessionsPerWeek = 3, CurriculumId = Guid.NewGuid() };
            await FluentActions.Invoking(() => _service.CreatePackageAsync(req))
                .Should().ThrowAsync<ArgumentException>();
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-10)]
        public async Task CreatePackageAsync_InvalidPrice_Throws(decimal price)
        {
            var req = new CreatePackageRequest { PackageName = "X", Grade = "grade 9", Price = price, SessionsPerWeek = 3, CurriculumId = Guid.NewGuid() };
            await FluentActions.Invoking(() => _service.CreatePackageAsync(req))
                .Should().ThrowAsync<ArgumentException>();
        }

        [Fact]
        public async Task CreatePackageAsync_InvalidSessionsPerWeek_Throws()
        {
            var req = new CreatePackageRequest { PackageName = "X", Grade = "grade 9", Price = 100, SessionsPerWeek = 2, CurriculumId = Guid.NewGuid() };
            await FluentActions.Invoking(() => _service.CreatePackageAsync(req))
                .Should().ThrowAsync<ArgumentException>();
        }

        [Fact]
        public async Task CreatePackageAsync_CurriculumNotFound_Throws()
        {
            var req = new CreatePackageRequest { PackageName = "X", Grade = "grade 9", Price = 100, SessionsPerWeek = 3, CurriculumId = Guid.NewGuid() };
            _repo.Setup(r => r.ExistsCurriculumAsync(req.CurriculumId)).ReturnsAsync(false);
            await FluentActions.Invoking(() => _service.CreatePackageAsync(req))
                .Should().ThrowAsync<KeyNotFoundException>();
        }

        [Fact]
        public async Task GetAllPackagesAsync_Maps()
        {
            _repo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<PaymentPackage>
            {
                new PaymentPackage { PackageId = Guid.NewGuid(), PackageName = "A", Grade = "grade 9", Price = 100 },
                new PaymentPackage { PackageId = Guid.NewGuid(), PackageName = "B", Grade = "grade 10", Price = 200 }
            });

            var list = await _service.GetAllPackagesAsync();
            list.Should().HaveCount(2);
        }

        [Fact]
        public async Task GetPackageByIdAsync_Maps()
        {
            var id = Guid.NewGuid();
            _repo.Setup(r => r.GetByIdAsync(id)).ReturnsAsync(new PaymentPackage
            {
                PackageId = id,
                PackageName = "P",
                Grade = "grade 9",
                Price = 100,
                SessionCount = 36,
                SessionsPerWeek = 3,
                MaxReschedule = 5,
                DurationDays = 90,
                Description = "",
                IsActive = true
            });

            var dto = await _service.GetPackageByIdAsync(id);
            dto.PackageId.Should().Be(id);
            dto.PackageName.Should().Be("P");
        }

        [Fact]
        public async Task UpdatePackageAsync_NotFound_Throws()
        {
            _repo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((PaymentPackage)null!);
            await FluentActions.Invoking(() => _service.UpdatePackageAsync(Guid.NewGuid(), new UpdatePackageRequest()))
                .Should().ThrowAsync<KeyNotFoundException>();
        }

        [Fact]
        public async Task UpdatePackageAsync_InvalidGrade_Throws()
        {
            var id = Guid.NewGuid();
            _repo.Setup(r => r.GetByIdAsync(id)).ReturnsAsync(new PaymentPackage { PackageId = id });
            var req = new UpdatePackageRequest { Grade = "foo" };
            await FluentActions.Invoking(() => _service.UpdatePackageAsync(id, req))
                .Should().ThrowAsync<ArgumentException>();
        }

        [Fact]
        public async Task UpdatePackageAsync_InvalidPrice_Throws()
        {
            var id = Guid.NewGuid();
            _repo.Setup(r => r.GetByIdAsync(id)).ReturnsAsync(new PaymentPackage { PackageId = id });
            var req = new UpdatePackageRequest { Price = 0 };
            await FluentActions.Invoking(() => _service.UpdatePackageAsync(id, req))
                .Should().ThrowAsync<ArgumentException>();
        }

        [Fact]
        public async Task UpdatePackageAsync_InvalidSessionsPerWeek_Throws()
        {
            var id = Guid.NewGuid();
            _repo.Setup(r => r.GetByIdAsync(id)).ReturnsAsync(new PaymentPackage { PackageId = id });
            var req = new UpdatePackageRequest { SessionsPerWeek = 2 };
            await FluentActions.Invoking(() => _service.UpdatePackageAsync(id, req))
                .Should().ThrowAsync<ArgumentException>();
        }

        [Fact]
        public async Task UpdatePackageAsync_CurriculumCheck_Performed()
        {
            var id = Guid.NewGuid();
            _repo.Setup(r => r.GetByIdAsync(id)).ReturnsAsync(new PaymentPackage { PackageId = id });
            _repo.Setup(r => r.ExistsCurriculumAsync(It.IsAny<Guid>())).ReturnsAsync(true);
            var req = new UpdatePackageRequest { CurriculumId = Guid.NewGuid(), PackageName = "New" };

            var dto = await _service.UpdatePackageAsync(id, req);
            dto.PackageName.Should().Be("New");
            _repo.Verify(r => r.UpdateAsync(It.IsAny<PaymentPackage>()), Times.Once);
        }

        [Fact]
        public async Task DeletePackageAsync_NotFound_Throws()
        {
            _repo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((PaymentPackage)null!);
            await FluentActions.Invoking(() => _service.DeletePackageAsync(Guid.NewGuid()))
                .Should().ThrowAsync<KeyNotFoundException>();
        }

        [Fact]
        public async Task DeletePackageAsync_InUse_Throws()
        {
            var id = Guid.NewGuid();
            _repo.Setup(r => r.GetByIdAsync(id)).ReturnsAsync(new PaymentPackage { PackageId = id });
            _repo.Setup(r => r.IsPackageInUseAsync(id)).ReturnsAsync(true);

            await FluentActions.Invoking(() => _service.DeletePackageAsync(id))
                .Should().ThrowAsync<InvalidOperationException>();
        }

        [Fact]
        public async Task DeletePackageAsync_Valid_Deletes()
        {
            var id = Guid.NewGuid();
            _repo.Setup(r => r.GetByIdAsync(id)).ReturnsAsync(new PaymentPackage { PackageId = id });
            _repo.Setup(r => r.IsPackageInUseAsync(id)).ReturnsAsync(false);
            _repo.Setup(r => r.DeleteAsync(id)).Returns(Task.CompletedTask);

            await _service.DeletePackageAsync(id);
            _repo.Verify(r => r.DeleteAsync(id), Times.Once);
        }

        [Fact]
        public async Task GetAllActivePackagesAsync_Maps()
        {
            _repo.Setup(r => r.GetAllActivePackagesAsync()).ReturnsAsync(new List<PaymentPackage> { new PaymentPackage { PackageId = Guid.NewGuid() } });
            var list = await _service.GetAllActivePackagesAsync();
            list.Should().HaveCount(1);
        }

        [Fact]
        public async Task GetActivePackageByIdAsync_NotFound_ReturnsNull()
        {
            _repo.Setup(r => r.GetActivePackageByIdAsync(It.IsAny<Guid>())).ReturnsAsync((PaymentPackage)null!);
            var dto = await _service.GetActivePackageByIdAsync(Guid.NewGuid());
            dto.Should().BeNull();
        }

        [Fact]
        public async Task GetActivePackageByIdAsync_Found_ReturnsDto()
        {
            var id = Guid.NewGuid();
            _repo.Setup(r => r.GetActivePackageByIdAsync(id)).ReturnsAsync(new PaymentPackage { PackageId = id, PackageName = "A" });
            var dto = await _service.GetActivePackageByIdAsync(id);
            dto.Should().NotBeNull();
            dto!.PackageId.Should().Be(id);
        }
    }
}