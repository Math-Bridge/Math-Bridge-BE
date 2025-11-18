using FluentAssertions;
using MathBridgeSystem.Api.Controllers;
using MathBridgeSystem.Application.DTOs.Statistics;
using MathBridgeSystem.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Moq;
using System;
using System.Collections.Generic;
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

        #region Session Statistics Tests

        [Fact]
        public async Task GetSessionStatistics_ReturnsOk()
        {
            var stats = new SessionStatisticsDto { TotalSessions = 10, CompletedSessions = 7 };
            _statisticsServiceMock.Setup(s => s.GetSessionStatisticsAsync()).ReturnsAsync(stats);

            var result = await _controller.GetSessionStatistics();

            var ok = result.Should().BeOfType<OkObjectResult>().Subject;
            ok.Value.Should().BeAssignableTo<SessionStatisticsDto>();
        }

        [Fact]
        public async Task GetSessionStatistics_ServiceThrows_Returns500()
        {
            _statisticsServiceMock.Setup(s => s.GetSessionStatisticsAsync()).ThrowsAsync(new Exception("boom"));

            var result = await _controller.GetSessionStatistics();

            var obj = result.Should().BeOfType<ObjectResult>().Subject;
            obj.StatusCode.Should().Be(500);
        }

        [Fact]
        public async Task GetSessionOnlineVsOffline_ReturnsOk()
        {
            var dto = new SessionOnlineVsOfflineDto { OnlineSessions = 5, OfflineSessions = 5, OnlinePercentage = 50, OfflinePercentage = 50 };
            _statisticsServiceMock.Setup(s => s.GetSessionOnlineVsOfflineAsync()).ReturnsAsync(dto);

            var result = await _controller.GetSessionOnlineVsOffline();

            var ok = result.Should().BeOfType<OkObjectResult>().Subject;
            ok.Value.Should().BeAssignableTo<SessionOnlineVsOfflineDto>();
        }

        [Fact]
        public async Task GetSessionOnlineVsOffline_ServiceThrows_Returns500()
        {
            _statisticsServiceMock.Setup(s => s.GetSessionOnlineVsOfflineAsync()).ThrowsAsync(new Exception("boom"));

            var result = await _controller.GetSessionOnlineVsOffline();

            var obj = result.Should().BeOfType<ObjectResult>().Subject;
            obj.StatusCode.Should().Be(500);
        }

        [Fact]
        public async Task GetSessionTrends_ValidDates_ReturnsOk()
        {
            var start = new DateTime(2024,1,1);
            var end = new DateTime(2024,1,31);
            var dto = new SessionTrendStatisticsDto { Trends = new List<SessionTrendDto> { new SessionTrendDto { Date = start, SessionCount = 1 } }, TotalSessionsInPeriod = 1 };
            _statisticsServiceMock.Setup(s => s.GetSessionTrendsAsync(start, end)).ReturnsAsync(dto);

            var result = await _controller.GetSessionTrends(start, end);

            var ok = result.Should().BeOfType<OkObjectResult>().Subject;
            ok.Value.Should().BeAssignableTo<SessionTrendStatisticsDto>();
        }

        [Fact]
        public async Task GetSessionTrends_InvalidDates_ReturnsBadRequest()
        {
            var start = new DateTime(2024,12,1);
            var end = new DateTime(2024,1,1);

            var result = await _controller.GetSessionTrends(start, end);

            result.Should().BeOfType<BadRequestObjectResult>();
        }

        [Fact]
        public async Task GetSessionTrends_ServiceThrows_Returns500()
        {
            var start = new DateTime(2024,1,1);
            var end = new DateTime(2024,1,31);
            _statisticsServiceMock.Setup(s => s.GetSessionTrendsAsync(start, end)).ThrowsAsync(new Exception("boom"));

            var result = await _controller.GetSessionTrends(start, end);

            var obj = result.Should().BeOfType<ObjectResult>().Subject;
            obj.StatusCode.Should().Be(500);
        }

        #endregion

        #region Tutor Statistics Tests

        [Fact]
        public async Task GetTutorStatistics_ReturnsOk()
        {
            var dto = new TutorStatisticsDto { TotalTutors = 20, AverageRating = 4.5m };
            _statisticsServiceMock.Setup(s => s.GetTutorStatisticsAsync()).ReturnsAsync(dto);

            var result = await _controller.GetTutorStatistics();

            var ok = result.Should().BeOfType<OkObjectResult>().Subject;
            ok.Value.Should().BeAssignableTo<TutorStatisticsDto>();
        }

        [Fact]
        public async Task GetTutorStatistics_ServiceThrows_Returns500()
        {
            _statisticsServiceMock.Setup(s => s.GetTutorStatisticsAsync()).ThrowsAsync(new Exception("boom"));

            var result = await _controller.GetTutorStatistics();

            var obj = result.Should().BeOfType<ObjectResult>().Subject;
            obj.StatusCode.Should().Be(500);
        }

        [Fact]
        public async Task GetTopRatedTutors_ValidLimit_ReturnsOk()
        {
            var dto = new TopRatedTutorsListDto { Tutors = new List<TopRatedTutorDto> { new TopRatedTutorDto() }, TotalTutors = 1 };
            _statisticsServiceMock.Setup(s => s.GetTopRatedTutorsAsync(5)).ReturnsAsync(dto);

            var result = await _controller.GetTopRatedTutors(5);

            var ok = result.Should().BeOfType<OkObjectResult>().Subject;
            ok.Value.Should().BeAssignableTo<TopRatedTutorsListDto>();
        }

        [Fact]
        public async Task GetTopRatedTutors_InvalidLimit_ReturnsBadRequest()
        {
            var result = await _controller.GetTopRatedTutors(0);
            result.Should().BeOfType<BadRequestObjectResult>();

            result = await _controller.GetTopRatedTutors(101);
            result.Should().BeOfType<BadRequestObjectResult>();
        }

        [Fact]
        public async Task GetTopRatedTutors_ServiceThrows_Returns500()
        {
            _statisticsServiceMock.Setup(s => s.GetTopRatedTutorsAsync(10)).ThrowsAsync(new Exception("boom"));

            var result = await _controller.GetTopRatedTutors(10);

            var obj = result.Should().BeOfType<ObjectResult>().Subject;
            obj.StatusCode.Should().Be(500);
        }

        [Fact]
        public async Task GetMostActiveTutors_ValidLimit_ReturnsOk()
        {
            var dto = new MostActiveTutorsListDto { Tutors = new List<TutorSessionCountDto> { new TutorSessionCountDto() }, TotalTutors = 1 };
            _statisticsServiceMock.Setup(s => s.GetMostActiveTutorsAsync(5)).ReturnsAsync(dto);

            var result = await _controller.GetMostActiveTutors(5);

            var ok = result.Should().BeOfType<OkObjectResult>().Subject;
            ok.Value.Should().BeAssignableTo<MostActiveTutorsListDto>();
        }

        [Fact]
        public async Task GetMostActiveTutors_InvalidLimit_ReturnsBadRequest()
        {
            var result = await _controller.GetMostActiveTutors(0);
            result.Should().BeOfType<BadRequestObjectResult>();

            result = await _controller.GetMostActiveTutors(101);
            result.Should().BeOfType<BadRequestObjectResult>();
        }

        [Fact]
        public async Task GetMostActiveTutors_ServiceThrows_Returns500()
        {
            _statisticsServiceMock.Setup(s => s.GetMostActiveTutorsAsync(10)).ThrowsAsync(new Exception("boom"));

            var result = await _controller.GetMostActiveTutors(10);

            var obj = result.Should().BeOfType<ObjectResult>().Subject;
            obj.StatusCode.Should().Be(500);
        }

        #endregion

        #region Financial Statistics Tests

        [Fact]
        public async Task GetRevenueStatistics_ReturnsOk()
        {
            var dto = new RevenueStatisticsDto { TotalRevenue = 10000m, TotalTransactions = 5 };
            _statisticsServiceMock.Setup(s => s.GetRevenueStatisticsAsync()).ReturnsAsync(dto);

            var result = await _controller.GetRevenueStatistics();

            var ok = result.Should().BeOfType<OkObjectResult>().Subject;
            ok.Value.Should().BeAssignableTo<RevenueStatisticsDto>();
        }

        [Fact]
        public async Task GetRevenueStatistics_ServiceThrows_Returns500()
        {
            _statisticsServiceMock.Setup(s => s.GetRevenueStatisticsAsync()).ThrowsAsync(new Exception("boom"));

            var result = await _controller.GetRevenueStatistics();

            var obj = result.Should().BeOfType<ObjectResult>().Subject;
            obj.StatusCode.Should().Be(500);
        }

        [Fact]
        public async Task GetRevenueTrends_ValidDates_ReturnsOk()
        {
            var start = new DateTime(2024,1,1);
            var end = new DateTime(2024,1,31);
            var dto = new RevenueTrendStatisticsDto { Trends = new List<RevenueTrendDto> { new RevenueTrendDto { Date = start, Revenue = 100m } }, TotalRevenueInPeriod = 100 };
            _statisticsServiceMock.Setup(s => s.GetRevenueTrendsAsync(start, end)).ReturnsAsync(dto);

            var result = await _controller.GetRevenueTrends(start, end);

            var ok = result.Should().BeOfType<OkObjectResult>().Subject;
            ok.Value.Should().BeAssignableTo<RevenueTrendStatisticsDto>();
        }

        [Fact]
        public async Task GetRevenueTrends_InvalidDates_ReturnsBadRequest()
        {
            var start = new DateTime(2024,12,1);
            var end = new DateTime(2024,1,1);

            var result = await _controller.GetRevenueTrends(start, end);

            result.Should().BeOfType<BadRequestObjectResult>();
        }

        [Fact]
        public async Task GetRevenueTrends_ServiceThrows_Returns500()
        {
            var start = new DateTime(2024,1,1);
            var end = new DateTime(2024,1,31);
            _statisticsServiceMock.Setup(s => s.GetRevenueTrendsAsync(start, end)).ThrowsAsync(new Exception("boom"));

            var result = await _controller.GetRevenueTrends(start, end);

            var obj = result.Should().BeOfType<ObjectResult>().Subject;
            obj.StatusCode.Should().Be(500);
        }

        #endregion
    }
}

