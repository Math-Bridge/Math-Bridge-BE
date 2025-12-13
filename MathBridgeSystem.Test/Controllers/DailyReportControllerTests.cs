//using MathBridgeSystem.Api.Controllers;
//using MathBridgeSystem.Application.DTOs.DailyReport;
//using MathBridgeSystem.Application.DTOs.Progress;
//using MathBridgeSystem.Application.Interfaces;
//using Microsoft.AspNetCore.Http;
//using Microsoft.AspNetCore.Mvc;
//using Moq;
//using System.Security.Claims;
//using Xunit;
//using Assert = Xunit.Assert;
//using System.Collections.Generic;

//namespace MathBridgeSystem.Tests.Controllers
//{
//    public class DailyReportControllerTests
//    {
//        private readonly Mock<IDailyReportService> _mockDailyReportService;
//        private readonly DailyReportController _controller;
//        private readonly Guid _userId = Guid.NewGuid();

//        public DailyReportControllerTests()
//        {
//            _mockDailyReportService = new Mock<IDailyReportService>();
//            _controller = new DailyReportController(_mockDailyReportService.Object);

//            var user = new ClaimsPrincipal(new ClaimsIdentity(new Claim[]
//            {
//                new Claim(ClaimTypes.NameIdentifier, _userId.ToString()),
//                new Claim("sub", _userId.ToString()),
//                new Claim(ClaimTypes.Role, "tutor")
//            }, "mock"));

//            _controller.ControllerContext = new ControllerContext()
//            {
//                HttpContext = new DefaultHttpContext() { User = user }
//            };
//        }

//        private void SetUserRole(string role)
//        {
//            var user = new ClaimsPrincipal(new ClaimsIdentity(new Claim[]
//            {
//                new Claim(ClaimTypes.NameIdentifier, _userId.ToString()),
//                new Claim("sub", _userId.ToString()),
//                new Claim(ClaimTypes.Role, role)
//            }, "mock"));

//            _controller.ControllerContext = new ControllerContext()
//            {
//                HttpContext = new DefaultHttpContext() { User = user }
//            };
//        }

//        [Fact]
//        public async Task GetDailyReportById_ValidId_ReturnsOk()
//        {
//            // Arrange
//            var reportId = Guid.NewGuid();
//            var dailyReport = new DailyReportDto { ReportId = reportId };
//            _mockDailyReportService.Setup(s => s.GetDailyReportByIdAsync(reportId))
//                .ReturnsAsync(dailyReport);

//            // Act
//            var result = await _controller.GetDailyReportById(reportId);

//            // Assert
//            var okResult = Assert.IsType<OkObjectResult>(result);
//            Assert.NotNull(okResult.Value);
//            _mockDailyReportService.Verify(s => s.GetDailyReportByIdAsync(reportId), Times.Once);
//        }

//        [Fact]
//        public async Task GetDailyReportById_NotFound_ReturnsNotFound()
//        {
//            // Arrange
//            var reportId = Guid.NewGuid();
//            _mockDailyReportService.Setup(s => s.GetDailyReportByIdAsync(reportId))
//                .ThrowsAsync(new KeyNotFoundException("Report not found"));

//            // Act
//            var result = await _controller.GetDailyReportById(reportId);

//            // Assert
//            Assert.IsType<NotFoundObjectResult>(result);
//        }

//        [Fact]
//        public async Task GetDailyReportById_Exception_ReturnsInternalServerError()
//        {
//            // Arrange
//            var reportId = Guid.NewGuid();
//            _mockDailyReportService.Setup(s => s.GetDailyReportByIdAsync(reportId))
//                .ThrowsAsync(new Exception("Database error"));

//            // Act
//            var result = await _controller.GetDailyReportById(reportId);

//            // Assert
//            var objectResult = Assert.IsType<ObjectResult>(result);
//            Assert.Equal(500, objectResult.StatusCode);
//        }

//        [Fact]
//        public async Task GetDailyReportsByTutor_ReturnsOk()
//        {
//            // Arrange
//            SetUserRole("tutor");
//            var reports = new List<DailyReportDto> { new DailyReportDto { ReportId = Guid.NewGuid(), TutorId = _userId } };
//            _mockDailyReportService.Setup(s => s.GetDailyReportsByTutorIdAsync(_userId)).ReturnsAsync(reports);

//            // Act
//            var result = await _controller.GetDailyReportsByTutor();

//            // Assert
//            var okResult = Assert.IsType<OkObjectResult>(result);
//            Assert.NotNull(okResult.Value);
//        }

//        [Fact]
//        public async Task GetDailyReportsByTutor_Exception_ReturnsInternalServerError()
//        {
//            // Arrange
//            SetUserRole("tutor");
//            _mockDailyReportService.Setup(s => s.GetDailyReportsByTutorIdAsync(_userId)).ThrowsAsync(new Exception("boom"));

//            // Act
//            var result = await _controller.GetDailyReportsByTutor();

//            // Assert
//            var objectResult = Assert.IsType<ObjectResult>(result);
//            Assert.Equal(500, objectResult.StatusCode);
//        }

//        [Fact]
//        public async Task GetDailyReportsByChild_ReturnsOk()
//        {
//            // Arrange
//            var childId = Guid.NewGuid();
//            var reports = new List<DailyReportDto> { new DailyReportDto { ReportId = Guid.NewGuid(), ChildId = childId } };
//            _mockDailyReportService.Setup(s => s.GetDailyReportsByChildIdAsync(childId)).ReturnsAsync(reports);

//            // Act
//            var result = await _controller.GetDailyReportsByChild(childId);

//            // Assert
//            var okResult = Assert.IsType<OkObjectResult>(result);
//            Assert.NotNull(okResult.Value);
//        }

//        [Fact]
//        public async Task GetDailyReportsByChild_Exception_ReturnsInternalServerError()
//        {
//            // Arrange
//            var childId = Guid.NewGuid();
//            _mockDailyReportService.Setup(s => s.GetDailyReportsByChildIdAsync(childId)).ThrowsAsync(new Exception("boom"));

//            // Act
//            var result = await _controller.GetDailyReportsByChild(childId);

//            // Assert
//            var objectResult = Assert.IsType<ObjectResult>(result);
//            Assert.Equal(500, objectResult.StatusCode);
//        }

//        [Fact]
//        public async Task GetDailyReportsByBooking_ReturnsOk()
//        {
//            // Arrange
//            var bookingId = Guid.NewGuid();
//            var reports = new List<DailyReportDto> { new DailyReportDto { ReportId = Guid.NewGuid(), BookingId = bookingId } };
//            _mockDailyReportService.Setup(s => s.GetDailyReportsByBookingIdAsync(bookingId)).ReturnsAsync(reports);

//            // Act
//            var result = await _controller.GetDailyReportsByBooking(bookingId);

//            // Assert
//            var okResult = Assert.IsType<OkObjectResult>(result);
//            Assert.NotNull(okResult.Value);
//        }

//        [Fact]
//        public async Task GetDailyReportsByBooking_Exception_ReturnsInternalServerError()
//        {
//            // Arrange
//            var bookingId = Guid.NewGuid();
//            _mockDailyReportService.Setup(s => s.GetDailyReportsByBookingIdAsync(bookingId)).ThrowsAsync(new Exception("boom"));

//            // Act
//            var result = await _controller.GetDailyReportsByBooking(bookingId);

//            // Assert
//            var objectResult = Assert.IsType<ObjectResult>(result);
//            Assert.Equal(500, objectResult.StatusCode);
//        }

//        [Fact]
//        public async Task GetLearningCompletionForecast_Success_ReturnsOk()
//        {
//            // Arrange
//            var contractId = Guid.NewGuid();
//            var forecast = new LearningCompletionForecastDto { ContractId = contractId, ChildId = Guid.NewGuid(), Message = "ok" };
//            _mockDailyReportService.Setup(s => s.GetLearningCompletionForecastAsync(contractId)).ReturnsAsync(forecast);

//            // Act
//            var result = await _controller.GetLearningCompletionForecast(contractId);

//            // Assert
//            var okResult = Assert.IsType<OkObjectResult>(result);
//            Assert.NotNull(okResult.Value);
//        }

//        [Fact]
//        public async Task GetLearningCompletionForecast_NotFound_ReturnsNotFound()
//        {
//            // Arrange
//            var contractId = Guid.NewGuid();
//            _mockDailyReportService.Setup(s => s.GetLearningCompletionForecastAsync(contractId)).ThrowsAsync(new KeyNotFoundException("no data"));

//            // Act
//            var result = await _controller.GetLearningCompletionForecast(contractId);

//            // Assert
//            Assert.IsType<NotFoundObjectResult>(result);
//        }

//        [Fact]
//        public async Task GetLearningCompletionForecast_InvalidOperation_ReturnsBadRequest()
//        {
//            // Arrange
//            var contractId = Guid.NewGuid();
//            _mockDailyReportService.Setup(s => s.GetLearningCompletionForecastAsync(contractId)).ThrowsAsync(new InvalidOperationException("bad"));

//            // Act
//            var result = await _controller.GetLearningCompletionForecast(contractId);

//            // Assert
//            Assert.IsType<BadRequestObjectResult>(result);
//        }

//        [Fact]
//        public async Task GetLearningCompletionForecast_Exception_ReturnsInternalServerError()
//        {
//            // Arrange
//            var contractId = Guid.NewGuid();
//            _mockDailyReportService.Setup(s => s.GetLearningCompletionForecastAsync(contractId)).ThrowsAsync(new Exception("boom"));

//            // Act
//            var result = await _controller.GetLearningCompletionForecast(contractId);

//            // Assert
//            var objectResult = Assert.IsType<ObjectResult>(result);
//            Assert.Equal(500, objectResult.StatusCode);
//        }

//        [Fact]
//        public async Task GetChildUnitProgress_Success_ReturnsOk()
//        {
//            // Arrange
//            var childId = Guid.NewGuid();
//            var progress = new ChildUnitProgressDto { ChildId = childId, ChildName = "C", Message = "m" };
//            _mockDailyReportService.Setup(s => s.GetChildUnitProgressAsync(childId)).ReturnsAsync(progress);

//            // Act
//            var result = await _controller.GetChildUnitProgress(childId);

//            // Assert
//            var okResult = Assert.IsType<OkObjectResult>(result);
//            Assert.NotNull(okResult.Value);
//        }

//        [Fact]
//        public async Task GetChildUnitProgress_NotFound_ReturnsNotFound()
//        {
//            // Arrange
//            var childId = Guid.NewGuid();
//            _mockDailyReportService.Setup(s => s.GetChildUnitProgressAsync(childId)).ThrowsAsync(new KeyNotFoundException("no"));

//            // Act
//            var result = await _controller.GetChildUnitProgress(childId);

//            // Assert
//            Assert.IsType<NotFoundObjectResult>(result);
//        }

//        [Fact]
//        public async Task GetChildUnitProgress_InvalidOperation_ReturnsBadRequest()
//        {
//            // Arrange
//            var childId = Guid.NewGuid();
//            _mockDailyReportService.Setup(s => s.GetChildUnitProgressAsync(childId)).ThrowsAsync(new InvalidOperationException("bad"));

//            // Act
//            var result = await _controller.GetChildUnitProgress(childId);

//            // Assert
//            Assert.IsType<BadRequestObjectResult>(result);
//        }

//        [Fact]
//        public async Task GetChildUnitProgress_Exception_ReturnsInternalServerError()
//        {
//            // Arrange
//            var childId = Guid.NewGuid();
//            _mockDailyReportService.Setup(s => s.GetChildUnitProgressAsync(childId)).ThrowsAsync(new Exception("boom"));

//            // Act
//            var result = await _controller.GetChildUnitProgress(childId);

//            // Assert
//            var objectResult = Assert.IsType<ObjectResult>(result);
//            Assert.Equal(500, objectResult.StatusCode);
//        }

//        [Fact]
//        public async Task CreateDailyReport_Valid_ReturnsCreated()
//        {
//            // Arrange
//            SetUserRole("tutor");
//            var request = new CreateDailyReportRequest { ChildId = Guid.NewGuid(), BookingId = Guid.NewGuid(), OnTrack = true, HaveHomework = false, UnitId = Guid.NewGuid() };
//            var reportId = Guid.NewGuid();
//            _mockDailyReportService.Setup(s => s.CreateDailyReportAsync(request, _userId)).ReturnsAsync(reportId);

//            // Act
//            var result = await _controller.CreateDailyReport(request);

//            // Assert
//            var created = Assert.IsType<CreatedAtActionResult>(result);
//            Assert.NotNull(created.Value);
//        }

//        [Fact]
//        public async Task CreateDailyReport_InvalidModel_ReturnsBadRequest()
//        {
//            // Arrange
//            SetUserRole("tutor");
//            _controller.ModelState.AddModelError("ChildId", "Required");

//            // Act
//            var result = await _controller.CreateDailyReport(new CreateDailyReportRequest());

//            // Assert
//            Assert.IsType<BadRequestObjectResult>(result);
//        }

//        [Fact]
//        public async Task CreateDailyReport_ArgumentException_ReturnsBadRequest()
//        {
//            // Arrange
//            SetUserRole("tutor");
//            var request = new CreateDailyReportRequest { ChildId = Guid.NewGuid(), BookingId = Guid.NewGuid(), OnTrack = true, HaveHomework = false, UnitId = Guid.NewGuid() };
//            _mockDailyReportService.Setup(s => s.CreateDailyReportAsync(request, _userId)).ThrowsAsync(new ArgumentException("invalid"));

//            // Act
//            var result = await _controller.CreateDailyReport(request);

//            // Assert
//            Assert.IsType<BadRequestObjectResult>(result);
//        }

//        [Fact]
//        public async Task CreateDailyReport_Exception_ReturnsInternalServerError()
//        {
//            // Arrange
//            SetUserRole("tutor");
//            var request = new CreateDailyReportRequest { ChildId = Guid.NewGuid(), BookingId = Guid.NewGuid(), OnTrack = true, HaveHomework = false, UnitId = Guid.NewGuid() };
//            _mockDailyReportService.Setup(s => s.CreateDailyReportAsync(request, _userId)).ThrowsAsync(new Exception("boom"));

//            // Act
//            var result = await _controller.CreateDailyReport(request);

//            // Assert
//            var objectResult = Assert.IsType<ObjectResult>(result);
//            Assert.Equal(500, objectResult.StatusCode);
//        }

//        [Fact]
//        public async Task UpdateDailyReport_Success_ReturnsOk()
//        {
//            // Arrange
//            var reportId = Guid.NewGuid();
//            var request = new UpdateDailyReportRequest { Notes = "n" };
//            _mockDailyReportService.Setup(s => s.UpdateDailyReportAsync(reportId, request)).Returns(Task.CompletedTask);

//            // Act
//            var result = await _controller.UpdateDailyReport(reportId, request);

//            // Assert
//            var okResult = Assert.IsType<OkObjectResult>(result);
//            Assert.NotNull(okResult.Value);
//        }

//        [Fact]
//        public async Task UpdateDailyReport_InvalidModel_ReturnsBadRequest()
//        {
//            // Arrange
//            _controller.ModelState.AddModelError("Notes", "Max");

//            // Act
//            var result = await _controller.UpdateDailyReport(Guid.NewGuid(), new UpdateDailyReportRequest());

//            // Assert
//            Assert.IsType<BadRequestObjectResult>(result);
//        }

//        [Fact]
//        public async Task UpdateDailyReport_NotFound_ReturnsNotFound()
//        {
//            // Arrange
//            var reportId = Guid.NewGuid();
//            var request = new UpdateDailyReportRequest();
//            _mockDailyReportService.Setup(s => s.UpdateDailyReportAsync(reportId, request)).ThrowsAsync(new KeyNotFoundException());

//            // Act
//            var result = await _controller.UpdateDailyReport(reportId, request);

//            // Assert
//            Assert.IsType<NotFoundObjectResult>(result);
//        }

//        [Fact]
//        public async Task UpdateDailyReport_ArgumentException_ReturnsBadRequest()
//        {
//            // Arrange
//            var reportId = Guid.NewGuid();
//            var request = new UpdateDailyReportRequest();
//            _mockDailyReportService.Setup(s => s.UpdateDailyReportAsync(reportId, request)).ThrowsAsync(new ArgumentException("bad"));

//            // Act
//            var result = await _controller.UpdateDailyReport(reportId, request);

//            // Assert
//            Assert.IsType<BadRequestObjectResult>(result);
//        }

//        [Fact]
//        public async Task UpdateDailyReport_Exception_ReturnsInternalServerError()
//        {
//            // Arrange
//            var reportId = Guid.NewGuid();
//            var request = new UpdateDailyReportRequest();
//            _mockDailyReportService.Setup(s => s.UpdateDailyReportAsync(reportId, request)).ThrowsAsync(new Exception("boom"));

//            // Act
//            var result = await _controller.UpdateDailyReport(reportId, request);

//            // Assert
//            var objectResult = Assert.IsType<ObjectResult>(result);
//            Assert.Equal(500, objectResult.StatusCode);
//        }

//        [Fact]
//        public async Task DeleteDailyReport_Success_ReturnsOk()
//        {
//            // Arrange
//            var reportId = Guid.NewGuid();
//            _mockDailyReportService.Setup(s => s.DeleteDailyReportAsync(reportId)).ReturnsAsync(true);

//            // Act
//            var result = await _controller.DeleteDailyReport(reportId);

//            // Assert
//            var okResult = Assert.IsType<OkObjectResult>(result);
//            Assert.NotNull(okResult.Value);
//        }

//        [Fact]
//        public async Task DeleteDailyReport_NotFound_ReturnsNotFound()
//        {
//            // Arrange
//            var reportId = Guid.NewGuid();
//            _mockDailyReportService.Setup(s => s.DeleteDailyReportAsync(reportId)).ReturnsAsync(false);

//            // Act
//            var result = await _controller.DeleteDailyReport(reportId);

//            // Assert
//            Assert.IsType<NotFoundObjectResult>(result);
//        }

//        [Fact]
//        public async Task DeleteDailyReport_KeyNotFound_ReturnsNotFound()
//        {
//            // Arrange
//            var reportId = Guid.NewGuid();
//            _mockDailyReportService.Setup(s => s.DeleteDailyReportAsync(reportId)).ThrowsAsync(new KeyNotFoundException());

//            // Act
//            var result = await _controller.DeleteDailyReport(reportId);

//            // Assert
//            Assert.IsType<NotFoundObjectResult>(result);
//        }

//        [Fact]
//        public async Task DeleteDailyReport_Exception_ReturnsInternalServerError()
//        {
//            // Arrange
//            var reportId = Guid.NewGuid();
//            _mockDailyReportService.Setup(s => s.DeleteDailyReportAsync(reportId)).ThrowsAsync(new Exception("boom"));

//            // Act
//            var result = await _controller.DeleteDailyReport(reportId);

//            // Assert
//            var objectResult = Assert.IsType<ObjectResult>(result);
//            Assert.Equal(500, objectResult.StatusCode);
//        }

//        [Fact]
//        public void Constructor_NullDailyReportService_ThrowsArgumentNullException()
//        {
//            // Assert
//            Assert.Throws<ArgumentNullException>(() =>
//                new DailyReportController(null!));
//        }
//    }
//}
