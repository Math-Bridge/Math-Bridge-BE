using MathBridgeSystem.Api.Controllers;
using MathBridgeSystem.Application.DTOs.DailyReport;
using MathBridgeSystem.Application.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using System.Security.Claims;
using Xunit;
using Assert = Xunit.Assert;

namespace MathBridgeSystem.Test.Controllers
{
    public class DailyReportControllerTests
    {
        private readonly Mock<IDailyReportService> _mockDailyReportService;
        private readonly DailyReportController _controller;
        private readonly Guid _userId = Guid.NewGuid();

        public DailyReportControllerTests()
        {
            _mockDailyReportService = new Mock<IDailyReportService>();
            _controller = new DailyReportController(_mockDailyReportService.Object);

            var user = new ClaimsPrincipal(new ClaimsIdentity(new Claim[]
            {
                new Claim(ClaimTypes.NameIdentifier, _userId.ToString()),
                new Claim("sub", _userId.ToString())
            }, "mock"));

            _controller.ControllerContext = new ControllerContext()
            {
                HttpContext = new DefaultHttpContext() { User = user }
            };
        }

        [Fact]
        public async Task GetDailyReportById_ValidId_ReturnsOk()
        {
            // Arrange
            var reportId = Guid.NewGuid();
            var dailyReport = new DailyReportDto { ReportId = reportId };
            _mockDailyReportService.Setup(s => s.GetDailyReportByIdAsync(reportId))
                .ReturnsAsync(dailyReport);

            // Act
            var result = await _controller.GetDailyReportById(reportId);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.NotNull(okResult.Value);
            _mockDailyReportService.Verify(s => s.GetDailyReportByIdAsync(reportId), Times.Once);
        }

        [Fact]
        public async Task GetDailyReportById_NotFound_ReturnsNotFound()
        {
            // Arrange
            var reportId = Guid.NewGuid();
            _mockDailyReportService.Setup(s => s.GetDailyReportByIdAsync(reportId))
                .ThrowsAsync(new KeyNotFoundException("Report not found"));

            // Act
            var result = await _controller.GetDailyReportById(reportId);

            // Assert
            Assert.IsType<NotFoundObjectResult>(result);
        }

        [Fact]
        public async Task GetDailyReportById_Exception_ReturnsInternalServerError()
        {
            // Arrange
            var reportId = Guid.NewGuid();
            _mockDailyReportService.Setup(s => s.GetDailyReportByIdAsync(reportId))
                .ThrowsAsync(new Exception("Database error"));

            // Act
            var result = await _controller.GetDailyReportById(reportId);

            // Assert
            var objectResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(500, objectResult.StatusCode);
        }



        [Fact]
        public void Constructor_NullDailyReportService_ThrowsArgumentNullException()
        {
            // Assert
            Assert.Throws<ArgumentNullException>(() =>
                new DailyReportController(null!));
        }
    }
}
