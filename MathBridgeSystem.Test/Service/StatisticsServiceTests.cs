using FluentAssertions;
using MathBridgeSystem.Application.DTOs.Statistics;
using MathBridgeSystem.Application.Interfaces;
using MathBridgeSystem.Application.Services;
using MathBridgeSystem.Domain.Entities;
using MathBridgeSystem.Domain.Interfaces;
using Moq;
using Xunit;

namespace MathBridgeSystem.Tests.Services
{
    public class StatisticsServiceTests
    {
        private readonly Mock<IUserRepository> _userRepositoryMock;
        private readonly Mock<ISessionRepository> _sessionRepositoryMock;
        private readonly Mock<IFinalFeedbackRepository> _finalFeedbackRepositoryMock;
        private readonly Mock<IWalletTransactionRepository> _walletTransactionRepositoryMock;
        private readonly Mock<IContractRepository> _contractRepositoryMock;
        private readonly Mock<IPackageRepository> _packageRepositoryMock;
        private readonly Mock<ISePayRepository> _sePayRepositoryMock;
        private readonly StatisticsService _service;

        public StatisticsServiceTests()
        {
            _userRepositoryMock = new Mock<IUserRepository>();
            _sessionRepositoryMock = new Mock<ISessionRepository>();
            _finalFeedbackRepositoryMock = new Mock<IFinalFeedbackRepository>();
            _walletTransactionRepositoryMock = new Mock<IWalletTransactionRepository>();
            _contractRepositoryMock = new Mock<IContractRepository>();
            _packageRepositoryMock = new Mock<IPackageRepository>();
            _sePayRepositoryMock = new Mock<ISePayRepository>();

            _service = new StatisticsService(
                _userRepositoryMock.Object,
                _sessionRepositoryMock.Object,
                _finalFeedbackRepositoryMock.Object,
                _walletTransactionRepositoryMock.Object,
                _contractRepositoryMock.Object,
                _packageRepositoryMock.Object,
                _sePayRepositoryMock.Object
            );
        }

        [Fact]
        public async Task GetUserStatisticsAsync_ShouldReturnCorrectStatistics()
        {
            // Arrange
            var now = DateTime.UtcNow.ToLocalTime();
            var users = new List<User>
            {
                new User { UserId = Guid.NewGuid(), Role = new Role { RoleName = "parent" }, LastActive = now.AddHours(-1) },
                new User { UserId = Guid.NewGuid(), Role = new Role { RoleName = "parent" }, LastActive = now.AddDays(-3) },
                new User { UserId = Guid.NewGuid(), Role = new Role { RoleName = "admin" }, LastActive = now.AddDays(-10) },
                new User { UserId = Guid.NewGuid(), Role = new Role { RoleName = "staff" }, LastActive = now.AddDays(-20) }
            };

            var tutors = new List<User>
            {
                new User { UserId = Guid.NewGuid(), Role = new Role { RoleName = "tutor" } },
                new User { UserId = Guid.NewGuid(), Role = new Role { RoleName = "tutor" } }
            };

            _userRepositoryMock.Setup(r => r.GetAllAsync()).ReturnsAsync(users);
            _userRepositoryMock.Setup(r => r.GetTutorsAsync()).ReturnsAsync(tutors);

            // Act
            var result = await _service.GetUserStatisticsAsync();

            // Assert
            result.Should().NotBeNull();
            result.TotalUsers.Should().Be(4);
            result.TotalParents.Should().Be(2);
            result.TotalTutors.Should().Be(2);
            result.TotalAdmin.Should().Be(1);
            result.TotalStaff.Should().Be(1);
            result.ActiveUsersLast24Hours.Should().Be(1);
            result.ActiveUsersLastWeek.Should().Be(2);
            result.ActiveUsersLastMonth.Should().BeGreaterThanOrEqualTo(3);
        }

        [Fact]
        public async Task GetUserRegistrationTrendsAsync_ShouldReturnTrendsInPeriod()
        {
            // Arrange
            var startDate = new DateTime(2025, 1, 1);
            var endDate = new DateTime(2025, 1, 31);
            
            var users = new List<User>
            {
                new User { UserId = Guid.NewGuid(), CreatedDate = new DateTime(2025, 1, 5) },
                new User { UserId = Guid.NewGuid(), CreatedDate = new DateTime(2025, 1, 5) },
                new User { UserId = Guid.NewGuid(), CreatedDate = new DateTime(2025, 1, 10) },
                new User { UserId = Guid.NewGuid(), CreatedDate = new DateTime(2024, 12, 25) } // Outside range
            };

            _userRepositoryMock.Setup(r => r.GetAllAsync()).ReturnsAsync(users);

            // Act
            var result = await _service.GetUserRegistrationTrendsAsync(startDate, endDate);

            // Assert
            result.Should().NotBeNull();
            result.TotalNewUsersInPeriod.Should().Be(3);
            result.Trends.Should().HaveCount(2);
            result.Trends.First(t => t.Date == new DateTime(2025, 1, 5)).NewUsers.Should().Be(2);
            result.Trends.First(t => t.Date == new DateTime(2025, 1, 10)).NewUsers.Should().Be(1);
        }

        [Fact]
        public async Task GetUserLocationDistributionAsync_ShouldReturnCityDistribution()
        {
            // Arrange
            var users = new List<User>
            {
                new User { UserId = Guid.NewGuid(), City = "Seoul" },
                new User { UserId = Guid.NewGuid(), City = "Seoul" },
                new User { UserId = Guid.NewGuid(), City = "Busan" },
                new User { UserId = Guid.NewGuid(), City = null } // No city
            };

            _userRepositoryMock.Setup(r => r.GetAllAsync()).ReturnsAsync(users);

            // Act
            var result = await _service.GetUserLocationDistributionAsync();

            // Assert
            result.Should().NotBeNull();
            result.CityDistribution.Should().HaveCount(2);
            result.CityDistribution.First(c => c.City == "Seoul").UserCount.Should().Be(2);
            result.CityDistribution.First(c => c.City == "Busan").UserCount.Should().Be(1);
        }

        [Fact]
        public void Constructor_ShouldThrowArgumentNullException_WhenUserRepositoryIsNull()
        {
            // Act & Assert
            var action = () => new StatisticsService(
                null!,
                _sessionRepositoryMock.Object,
                _finalFeedbackRepositoryMock.Object,
                _walletTransactionRepositoryMock.Object,
                _contractRepositoryMock.Object,
                _packageRepositoryMock.Object,
                _sePayRepositoryMock.Object
            );

            action.Should().Throw<ArgumentNullException>();
        }
    }
}

