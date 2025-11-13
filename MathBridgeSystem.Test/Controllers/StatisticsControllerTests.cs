using FluentAssertions;
using MathBridgeSystem.Api.Controllers;
using MathBridgeSystem.Application.DTOs.Statistics;
using MathBridgeSystem.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace MathBridgeSystem.Tests.Controllers
{
    public class StatisticsControllerTests
    {
        private readonly Mock<IStatisticsService> _statisticsServiceMock;
        private readonly StatisticsController _controller;

        public StatisticsControllerTests()
        {
            _statisticsServiceMock = new Mock<IStatisticsService>();
            _controller = new StatisticsController(_statisticsServiceMock.Object);
        }

        #region Constructor Tests

        [Fact]
        public void Constructor_NullStatisticsService_ThrowsArgumentNullException()
        {
            Action act = () => new StatisticsController(null!);
            act.Should().Throw<ArgumentNullException>().WithParameterName("statisticsService");
        }

        #endregion

        #region GetUserStatistics Tests

        [Fact]
        public async Task GetUserStatistics_ReturnsOkResult_WithStatistics()
        {
            // Arrange
            var expectedStats = new UserStatisticsDto
            {
                TotalUsers = 100,
                ActiveUsersLast24Hours = 80,
                TotalParents = 50,
                TotalTutors = 30,
                TotalStaff = 10,
                TotalAdmin = 10
            };
            _statisticsServiceMock.Setup(s => s.GetUserStatisticsAsync())
                .ReturnsAsync(expectedStats);

            // Act
            var result = await _controller.GetUserStatistics();

            // Assert
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            var stats = okResult.Value.Should().BeAssignableTo<UserStatisticsDto>().Subject;
            stats.TotalUsers.Should().Be(100);
            stats.ActiveUsersLast24Hours.Should().Be(80);
        }

        [Fact]
        public async Task GetUserStatistics_ServiceThrowsException_ReturnsInternalServerError()
        {
            // Arrange
            _statisticsServiceMock.Setup(s => s.GetUserStatisticsAsync())
                .ThrowsAsync(new Exception("Database error"));

            // Act
            var result = await _controller.GetUserStatistics();

            // Assert
            var statusResult = result.Should().BeOfType<ObjectResult>().Subject;
            statusResult.StatusCode.Should().Be(500);
        }

        #endregion

        #region GetUserRegistrationTrends Tests

        [Fact]
        public async Task GetUserRegistrationTrends_ValidDates_ReturnsOkResult()
        {
            // Arrange
            var startDate = new DateTime(2024, 1, 1);
            var endDate = new DateTime(2024, 12, 31);
            var expectedTrends = new UserRegistrationTrendStatisticsDto
            {
                Trends = new List<UserRegistrationTrendDto>
                {
                    new UserRegistrationTrendDto { Date = startDate, NewUsers = 10 },
                    new UserRegistrationTrendDto { Date = startDate.AddDays(1), NewUsers = 15 }
                },
                TotalNewUsersInPeriod = 25
            };
            _statisticsServiceMock.Setup(s => s.GetUserRegistrationTrendsAsync(startDate, endDate))
                .ReturnsAsync(expectedTrends);

            // Act
            var result = await _controller.GetUserRegistrationTrends(startDate, endDate);

            // Assert
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            var trends = okResult.Value.Should().BeAssignableTo<UserRegistrationTrendStatisticsDto>().Subject;
            trends.Trends.Should().HaveCount(2);
        }

        [Fact]
        public async Task GetUserRegistrationTrends_InvalidDateRange_ReturnsBadRequest()
        {
            // Arrange
            var startDate = new DateTime(2024, 12, 31);
            var endDate = new DateTime(2024, 1, 1);

            // Act
            var result = await _controller.GetUserRegistrationTrends(startDate, endDate);

            // Assert
            result.Should().BeOfType<BadRequestObjectResult>();
        }

        [Fact]
        public async Task GetUserRegistrationTrends_ServiceThrowsException_ReturnsInternalServerError()
        {
            // Arrange
            var startDate = new DateTime(2024, 1, 1);
            var endDate = new DateTime(2024, 12, 31);
            _statisticsServiceMock.Setup(s => s.GetUserRegistrationTrendsAsync(startDate, endDate))
                .ThrowsAsync(new Exception("Database error"));

            // Act
            var result = await _controller.GetUserRegistrationTrends(startDate, endDate);

            // Assert
            var statusResult = result.Should().BeOfType<ObjectResult>().Subject;
            statusResult.StatusCode.Should().Be(500);
        }

        #endregion

        #region GetUserLocationDistribution Tests

        [Fact]
        public async Task GetUserLocationDistribution_ReturnsOkResult_WithDistribution()
        {
            // Arrange
            var expectedDistribution = new UserLocationStatisticsDto
            {
                CityDistribution = new List<UserLocationDistributionDto>
                {
                    new UserLocationDistributionDto { City = "Ho Chi Minh City", UserCount = 50 },
                    new UserLocationDistributionDto { City = "Hanoi", UserCount = 30 }
                },
                TotalCities = 2
            };
            _statisticsServiceMock.Setup(s => s.GetUserLocationDistributionAsync())
                .ReturnsAsync(expectedDistribution);

            // Act
            var result = await _controller.GetUserLocationDistribution();

            // Assert
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            var distribution = okResult.Value.Should().BeAssignableTo<UserLocationStatisticsDto>().Subject;
            distribution.CityDistribution.Should().HaveCount(2);
        }

        [Fact]
        public async Task GetUserLocationDistribution_ServiceThrowsException_ReturnsInternalServerError()
        {
            // Arrange
            _statisticsServiceMock.Setup(s => s.GetUserLocationDistributionAsync())
                .ThrowsAsync(new Exception("Database error"));

            // Act
            var result = await _controller.GetUserLocationDistribution();

            // Assert
            var statusResult = result.Should().BeOfType<ObjectResult>().Subject;
            statusResult.StatusCode.Should().Be(500);
        }

        #endregion

        #region GetWalletStatistics Tests

        [Fact]
        public async Task GetWalletStatistics_ReturnsOkResult_WithStatistics()
        {
            // Arrange
            var expectedStats = new WalletStatisticsDto
            {
                TotalWalletBalance = 1000000,
                AverageWalletBalance = 20000,
                UsersWithPositiveBalance = 40
            };
            _statisticsServiceMock.Setup(s => s.GetWalletStatisticsAsync())
                .ReturnsAsync(expectedStats);

            // Act
            var result = await _controller.GetWalletStatistics();

            // Assert
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            var stats = okResult.Value.Should().BeAssignableTo<WalletStatisticsDto>().Subject;
            stats.TotalWalletBalance.Should().Be(1000000);
        }

        [Fact]
        public async Task GetWalletStatistics_ServiceThrowsException_ReturnsInternalServerError()
        {
            // Arrange
            _statisticsServiceMock.Setup(s => s.GetWalletStatisticsAsync())
                .ThrowsAsync(new Exception("Database error"));

            // Act
            var result = await _controller.GetWalletStatistics();

            // Assert
            var statusResult = result.Should().BeOfType<ObjectResult>().Subject;
            statusResult.StatusCode.Should().Be(500);
        }

        #endregion
    }
}

