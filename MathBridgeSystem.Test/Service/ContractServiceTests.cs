using FluentAssertions;
using MathBridgeSystem.Application.DTOs;
using MathBridgeSystem.Application.Services;
using MathBridgeSystem.Domain.Entities;
using MathBridgeSystem.Domain.Interfaces;
using Moq;
using Xunit;

namespace MathBridgeSystem.Tests.Services
{
    public class ContractServiceTests
    {
        private readonly Mock<IContractRepository> _contractRepositoryMock;
        private readonly Mock<IPackageRepository> _packageRepositoryMock;
        private readonly Mock<ISessionRepository> _sessionRepositoryMock;
        private readonly ContractService _contractService;

        public ContractServiceTests()
        {
            _contractRepositoryMock = new Mock<IContractRepository>();
            _packageRepositoryMock = new Mock<IPackageRepository>();
            _sessionRepositoryMock = new Mock<ISessionRepository>();
            _contractService = new ContractService(
                _contractRepositoryMock.Object,
                _packageRepositoryMock.Object,
                _sessionRepositoryMock.Object
            );
        }

        [Fact]
        public async Task CreateContractAsync_ValidRequest_ReturnsContractId()
        {
            // Arrange
            var request = new CreateContractRequest
            {
                ParentId = Guid.NewGuid(),
                ChildId = Guid.NewGuid(),
                PackageId = Guid.NewGuid(),
                StartDate = DateOnly.FromDateTime(DateTime.Now),
                EndDate = DateOnly.FromDateTime(DateTime.Now.AddDays(30)),
                DaysOfWeeks = 62, // Mon-Fri
                StartTime = TimeOnly.FromDateTime(DateTime.Now.AddHours(16)),
                EndTime = TimeOnly.FromDateTime(DateTime.Now.AddHours(18)),
                IsOnline = true,
                Status = "pending",
                MainTutorId = Guid.NewGuid()
            };
            var package = new PaymentPackage { SessionCount = 10, MaxReschedule = 2 };
            _packageRepositoryMock.Setup(repo => repo.GetByIdAsync(request.PackageId)).ReturnsAsync(package);
            _contractRepositoryMock.Setup(repo => repo.AddAsync(It.IsAny<Contract>())).Returns(Task.CompletedTask);
            _sessionRepositoryMock.Setup(repo => repo.AddRangeAsync(It.IsAny<IEnumerable<Session>>())).Returns(Task.CompletedTask);

            // Act
            var result = await _contractService.CreateContractAsync(request);

            // Assert
            result.Should().NotBe(Guid.Empty);
            _contractRepositoryMock.Verify(repo => repo.AddAsync(It.IsAny<Contract>()), Times.Once);
            _sessionRepositoryMock.Verify(repo => repo.AddRangeAsync(It.Is<IEnumerable<Session>>(s => s.Count() == 10)), Times.Once);
        }

        [Fact]
        public async Task CreateContractAsync_InvalidStatus_ThrowsArgumentException()
        {
            // Arrange
            var request = new CreateContractRequest { Status = "invalid" };

            // Act & Assert
            Func<Task> act = () => _contractService.CreateContractAsync(request);
            await act.Should().ThrowAsync<ArgumentException>().WithMessage("Invalid status.");
        }

        [Fact]
        public async Task UpdateContractStatusAsync_ValidUpdate_ReturnsTrue()
        {
            // Arrange
            var contractId = Guid.NewGuid();
            var request = new UpdateContractStatusRequest { Status = "active" };
            var contract = new Contract { Status = "pending" };
            _contractRepositoryMock.Setup(repo => repo.GetByIdAsync(contractId)).ReturnsAsync(contract);
            _contractRepositoryMock.Setup(repo => repo.UpdateAsync(It.IsAny<Contract>())).Returns(Task.CompletedTask);
            _sessionRepositoryMock.Setup(repo => repo.GetByContractIdAsync(contractId)).ReturnsAsync(new List<Session>());

            // Act
            var result = await _contractService.UpdateContractStatusAsync(contractId, request, Guid.NewGuid());

            // Assert
            result.Should().BeTrue();
            contract.Status.Should().Be("active");
        }

        [Fact]
        public async Task UpdateContractStatusAsync_InvalidNewStatus_ThrowsArgumentException()
        {
            // Arrange
            var contractId = Guid.NewGuid();
            var request = new UpdateContractStatusRequest { Status = "invalid" };
            _contractRepositoryMock.Setup(repo => repo.GetByIdAsync(contractId)).ReturnsAsync(new Contract());

            // Act & Assert
            Func<Task> act = () => _contractService.UpdateContractStatusAsync(contractId, request, Guid.NewGuid());
            await act.Should().ThrowAsync<ArgumentException>().WithMessage("Invalid status*");
        }

        [Fact]
        public async Task GetContractsByParentAsync_ReturnsDtoList()
        {
            // Arrange
            var parentId = Guid.NewGuid();
            var contracts = new List<Contract> { new Contract { Child = new Child { FullName = "Child" }, Package = new PaymentPackage { PackageName = "Package" }, MainTutor = new User { FullName = "Tutor" }, Center = new Center { Name = "Center" }, DaysOfWeeks = 62 } };
            _contractRepositoryMock.Setup(repo => repo.GetByParentIdAsync(parentId)).ReturnsAsync(contracts);

            // Act
            var result = await _contractService.GetContractsByParentAsync(parentId);

            // Assert
            result.Should().HaveCount(1);
            result[0].ChildName.Should().Be("Child");
            result[0].DaysOfWeeksDisplay.Should().Be("T2, T3, T4, T5, T6");
        }

        [Fact]
        public async Task AssignTutorsAsync_ValidAssignment_GeneratesSessions()
        {
            // Arrange
            var contractId = Guid.NewGuid();
            var request = new AssignTutorToContractRequest { MainTutorId = Guid.NewGuid() };
            var contract = new Contract { Package = new PaymentPackage { SessionCount = 5 }, StartDate = DateOnly.FromDateTime(DateTime.Now), EndDate = DateOnly.FromDateTime(DateTime.Now.AddDays(30)), DaysOfWeeks = 62, StartTime = TimeOnly.FromDateTime(DateTime.Now), EndTime = TimeOnly.FromDateTime(DateTime.Now.AddHours(2)), IsOnline = true, Status = "pending" };
            _contractRepositoryMock.Setup(repo => repo.GetByIdWithPackageAsync(contractId)).ReturnsAsync(contract);
            _contractRepositoryMock.Setup(repo => repo.UpdateAsync(It.IsAny<Contract>())).Returns(Task.CompletedTask);
            _sessionRepositoryMock.Setup(repo => repo.AddRangeAsync(It.IsAny<IEnumerable<Session>>())).Returns(Task.CompletedTask);

            // Act
            var result = await _contractService.AssignTutorsAsync(contractId, request, Guid.NewGuid());

            // Assert
            result.Should().BeTrue();
            _sessionRepositoryMock.Verify(repo => repo.AddRangeAsync(It.Is<IEnumerable<Session>>(s => s.Count() == 5)), Times.Once);
        }

        [Fact]
        public async Task GetAllContractsAsync_ReturnsDtoList()
        {
            // Arrange
            var contracts = new List<Contract> { new Contract { Child = new Child { FullName = "Child" }, Package = new PaymentPackage { PackageName = "Package" }, MainTutor = new User { FullName = "Tutor" }, Center = new Center { Name = "Center" }, DaysOfWeeks = 62 } };
            _contractRepositoryMock.Setup(repo => repo.GetAllWithDetailsAsync()).ReturnsAsync(contracts);

            // Act
            var result = await _contractService.GetAllContractsAsync();

            // Assert
            result.Should().HaveCount(1);
        }
    }
}