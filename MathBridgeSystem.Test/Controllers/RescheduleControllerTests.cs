using MathBridgeSystem.Api.Controllers;
using MathBridgeSystem.Application.DTOs;
using MathBridgeSystem.Application.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using System.Security.Claims;
using Xunit;
using Assert = Xunit.Assert;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MathBridgeSystem.Tests.Controllers
{
    public class RescheduleControllerTests
    {
        private readonly Mock<IRescheduleService> _mockService;
        private readonly RescheduleController _controller;
        private readonly Guid _userId = Guid.NewGuid();

        public RescheduleControllerTests()
        {
            _mockService = new Mock<IRescheduleService>();
            _controller = new RescheduleController(_mockService.Object);

            var user = new ClaimsPrincipal(new ClaimsIdentity(new Claim[]
            {
                new Claim(ClaimTypes.NameIdentifier, _userId.ToString()),
                new Claim("sub", _userId.ToString()),
                new Claim(ClaimTypes.Role, "parent")
            }, "mock"));

            _controller.ControllerContext = new ControllerContext()
            {
                HttpContext = new DefaultHttpContext() { User = user }
            };
        }

        private void SetUserRole(string role)
        {
            var user = new ClaimsPrincipal(new ClaimsIdentity(new Claim[]
            {
                new Claim(ClaimTypes.NameIdentifier, _userId.ToString()),
                new Claim("sub", _userId.ToString()),
                new Claim(ClaimTypes.Role, role)
            }, "mock"));

            _controller.ControllerContext = new ControllerContext()
            {
                HttpContext = new DefaultHttpContext() { User = user }
            };
        }

        [Fact]
        public async Task GetAll_AsStaff_ReturnsOk()
        {
            // Arrange
            SetUserRole("staff");
            var list = new List<RescheduleRequestDto> { new RescheduleRequestDto { RequestId = Guid.NewGuid() } };
            _mockService.Setup(s => s.GetAllAsync(null)).ReturnsAsync(list);

            // Act
            var result = await _controller.GetAll(null);

            // Assert
            var ok = Assert.IsType<OkObjectResult>(result);
            Assert.NotNull(ok.Value);
            _mockService.Verify(s => s.GetAllAsync(null), Times.Once);
        }

        [Fact]
        public async Task GetAll_AsParent_UsesUserId_ReturnsOk()
        {
            // Arrange
            SetUserRole("parent");
            var list = new List<RescheduleRequestDto> { new RescheduleRequestDto { RequestId = Guid.NewGuid(), ParentId = _userId } };
            _mockService.Setup(s => s.GetAllAsync(_userId)).ReturnsAsync(list);

            // Act
            var result = await _controller.GetAll(null);

            // Assert
            var ok = Assert.IsType<OkObjectResult>(result);
            Assert.NotNull(ok.Value);
            _mockService.Verify(s => s.GetAllAsync(_userId), Times.Once);
        }

        [Fact]
        public async Task GetAll_Exception_ReturnsBadRequest()
        {
            // Arrange
            SetUserRole("staff");
            _mockService.Setup(s => s.GetAllAsync(null)).ThrowsAsync(new Exception("boom"));

            // Act
            var result = await _controller.GetAll(null);

            // Assert
            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task GetById_Success_ReturnsOk()
        {
            // Arrange
            SetUserRole("staff");
            var id = Guid.NewGuid();
            var dto = new RescheduleRequestDto { RequestId = id };
            _mockService.Setup(s => s.GetByIdAsync(id, _userId, "staff")).ReturnsAsync(dto);

            // Act
            var result = await _controller.GetById(id);

            // Assert
            var ok = Assert.IsType<OkObjectResult>(result);
            Assert.NotNull(ok.Value);
        }

        [Fact]
        public async Task GetById_NotFound_ReturnsNotFound()
        {
            // Arrange
            SetUserRole("staff");
            var id = Guid.NewGuid();
            _mockService.Setup(s => s.GetByIdAsync(id, _userId, "staff")).ReturnsAsync((RescheduleRequestDto?)null);

            // Act
            var result = await _controller.GetById(id);

            // Assert
            Assert.IsType<NotFoundObjectResult>(result);
        }

        [Fact]
        public async Task GetById_Forbidden_ReturnsForbid()
        {
            // Arrange
            SetUserRole("parent");
            var id = Guid.NewGuid();
            _mockService.Setup(s => s.GetByIdAsync(id, _userId, "parent")).ThrowsAsync(new UnauthorizedAccessException("no"));

            // Act
            var result = await _controller.GetById(id);

            // Assert
            Assert.IsType<ForbidResult>(result);
        }

        [Fact]
        public async Task Create_Success_ReturnsOk()
        {
            // Arrange
            SetUserRole("parent");
            var request = new CreateRescheduleRequestDto { BookingId = Guid.NewGuid(), RequestedDate = DateOnly.FromDateTime(DateTime.Now.AddDays(1)), StartTime = new TimeOnly(16,0), EndTime = new TimeOnly(17,30) };
            var response = new RescheduleResponseDto { RequestId = Guid.NewGuid(), Status = "pending" };
            _mockService.Setup(s => s.CreateRequestAsync(_userId, request)).ReturnsAsync(response);

            // Act
            var result = await _controller.Create(request);

            // Assert
            var ok = Assert.IsType<OkObjectResult>(result);
            Assert.NotNull(ok.Value);
        }

        [Fact]
        public async Task Create_Exception_ReturnsBadRequest()
        {
            // Arrange
            SetUserRole("parent");
            var request = new CreateRescheduleRequestDto { BookingId = Guid.NewGuid(), RequestedDate = DateOnly.FromDateTime(DateTime.Now.AddDays(1)), StartTime = new TimeOnly(16,0), EndTime = new TimeOnly(17,30) };
            _mockService.Setup(s => s.CreateRequestAsync(_userId, request)).ThrowsAsync(new Exception("bad"));

            // Act
            var result = await _controller.Create(request);

            // Assert
            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task Approve_Success_ReturnsOk()
        {
            // Arrange
            SetUserRole("staff");
            var id = Guid.NewGuid();
            var dto = new ApproveRescheduleRequestDto { NewTutorId = Guid.Empty };
            var response = new RescheduleResponseDto { RequestId = id, Status = "approved" };
            _mockService.Setup(s => s.ApproveRequestAsync(_userId, id, dto)).ReturnsAsync(response);

            // Act
            var result = await _controller.Approve(id, dto);

            // Assert
            var ok = Assert.IsType<OkObjectResult>(result);
            Assert.NotNull(ok.Value);
        }

        [Fact]
        public async Task Approve_Exception_ReturnsBadRequest()
        {
            // Arrange
            SetUserRole("staff");
            var id = Guid.NewGuid();
            var dto = new ApproveRescheduleRequestDto { NewTutorId = Guid.Empty };
            _mockService.Setup(s => s.ApproveRequestAsync(_userId, id, dto)).ThrowsAsync(new Exception("bad"));

            // Act
            var result = await _controller.Approve(id, dto);

            // Assert
            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task GetAvailableSubTutors_Success_ReturnsOk()
        {
            // Arrange
            var reqId = Guid.NewGuid();
            var avail = new AvailableSubTutorsDto { AvailableTutors = new List<SubTutorInfoDto>(), TotalAvailable = 0 };
            _mockService.Setup(s => s.GetAvailableSubTutorsAsync(reqId)).ReturnsAsync(avail);

            // Act
            var result = await _controller.GetAvailableSubTutors(reqId);

            // Assert
            var ok = Assert.IsType<OkObjectResult>(result);
            Assert.NotNull(ok.Value);
        }

        [Fact]
        public async Task GetAvailableSubTutors_NotFound_ReturnsNotFound()
        {
            // Arrange
            var reqId = Guid.NewGuid();
            _mockService.Setup(s => s.GetAvailableSubTutorsAsync(reqId)).ThrowsAsync(new KeyNotFoundException("no"));

            // Act
            var result = await _controller.GetAvailableSubTutors(reqId);

            // Assert
            Assert.IsType<NotFoundObjectResult>(result);
        }

        [Fact]
        public async Task GetAvailableSubTutors_Exception_ReturnsBadRequest()
        {
            // Arrange
            var reqId = Guid.NewGuid();
            _mockService.Setup(s => s.GetAvailableSubTutorsAsync(reqId)).ThrowsAsync(new Exception("bad"));

            // Act
            var result = await _controller.GetAvailableSubTutors(reqId);

            // Assert
            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task Reject_Success_ReturnsOk()
        {
            // Arrange
            SetUserRole("staff");
            var id = Guid.NewGuid();
            var dto = new RejectRequestDto { Reason = "no" };
            var response = new RescheduleResponseDto { RequestId = id, Status = "rejected" };
            _mockService.Setup(s => s.RejectRequestAsync(_userId, id, dto.Reason)).ReturnsAsync(response);

            // Act
            var result = await _controller.Reject(id, dto);

            // Assert
            var ok = Assert.IsType<OkObjectResult>(result);
            Assert.NotNull(ok.Value);
        }

        [Fact]
        public async Task Reject_Exception_ReturnsBadRequest()
        {
            // Arrange
            SetUserRole("staff");
            var id = Guid.NewGuid();
            var dto = new RejectRequestDto { Reason = "no" };
            _mockService.Setup(s => s.RejectRequestAsync(_userId, id, dto.Reason)).ThrowsAsync(new Exception("bad"));

            // Act
            var result = await _controller.Reject(id, dto);

            // Assert
            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task CancelSessionAndRefund_Success_ReturnsOk()
        {
            // Arrange
            var sessionId = Guid.NewGuid();
            var reqId = Guid.NewGuid();
            var response = new RescheduleResponseDto { RequestId = sessionId, Status = "cancelled" };
            _mockService.Setup(s => s.CancelSessionAndRefundAsync(sessionId, reqId)).ReturnsAsync(response);

            // Act
            var result = await _controller.CancelSessionAndRefund(sessionId, reqId);

            // Assert
            var ok = Assert.IsType<OkObjectResult>(result);
            Assert.NotNull(ok.Value);
        }

        [Fact]
        public async Task CancelSessionAndRefund_NotFound_ReturnsNotFound()
        {
            // Arrange
            var sessionId = Guid.NewGuid();
            var reqId = Guid.NewGuid();
            _mockService.Setup(s => s.CancelSessionAndRefundAsync(sessionId, reqId)).ThrowsAsync(new KeyNotFoundException("no"));

            // Act
            var result = await _controller.CancelSessionAndRefund(sessionId, reqId);

            // Assert
            Assert.IsType<NotFoundObjectResult>(result);
        }

        [Fact]
        public async Task CancelSessionAndRefund_InvalidOperation_ReturnsBadRequest()
        {
            // Arrange
            var sessionId = Guid.NewGuid();
            var reqId = Guid.NewGuid();
            _mockService.Setup(s => s.CancelSessionAndRefundAsync(sessionId, reqId)).ThrowsAsync(new InvalidOperationException("bad"));

            // Act
            var result = await _controller.CancelSessionAndRefund(sessionId, reqId);

            // Assert
            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task CancelSessionAndRefund_Exception_ReturnsInternalServerError()
        {
            // Arrange
            var sessionId = Guid.NewGuid();
            var reqId = Guid.NewGuid();
            _mockService.Setup(s => s.CancelSessionAndRefundAsync(sessionId, reqId)).ThrowsAsync(new Exception("boom"));

            // Act
            var result = await _controller.CancelSessionAndRefund(sessionId, reqId);

            // Assert
            var objectResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(500, objectResult.StatusCode);
        }
    }
}
