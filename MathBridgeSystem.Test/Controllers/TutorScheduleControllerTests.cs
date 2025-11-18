using FluentAssertions;
using MathBridgeSystem.Api.Controllers;
using MathBridgeSystem.Application.DTOs;
using MathBridgeSystem.Application.DTOs.TutorSchedule;
using MathBridgeSystem.Application.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Xunit;
using Assert = Xunit.Assert;

namespace MathBridgeSystem.Tests.Controllers
{
    public class TutorScheduleControllerTests
    {
        private readonly Mock<ITutorScheduleService> _mockTutorScheduleService;
        private readonly TutorScheduleController _controller;

        public TutorScheduleControllerTests()
        {
            _mockTutorScheduleService = new Mock<ITutorScheduleService>();
            _controller = new TutorScheduleController(_mockTutorScheduleService.Object);
        }

        [Fact]
        public async Task CreateAvailability_ValidRequest_ReturnsCreatedResult()
        {
            // Arrange
            var tutorId = Guid.NewGuid();
            SetupUserClaims(tutorId, "tutor");
            var request = new CreateTutorScheduleRequest { TutorId = tutorId };
            var availabilityId = Guid.NewGuid();
            _mockTutorScheduleService.Setup(s => s.CreateAvailabilityAsync(request))
                .ReturnsAsync(availabilityId);

            // Act
            var result = await _controller.CreateAvailability(request);

            // Assert
            var createdResult = Assert.IsType<CreatedAtActionResult>(result);
            Assert.Equal(nameof(_controller.GetAvailabilityById), createdResult.ActionName);
            _mockTutorScheduleService.Verify(s => s.CreateAvailabilityAsync(request), Times.Once);
        }

        [Fact]
        public async Task CreateAvailability_TutorNotOwner_ReturnsForbid()
        {
            // Arrange
            var tutorId = Guid.NewGuid();
            var differentTutorId = Guid.NewGuid();
            SetupUserClaims(tutorId, "tutor");
            var request = new CreateTutorScheduleRequest { TutorId = differentTutorId };

            // Act
            var result = await _controller.CreateAvailability(request);

            // Assert
            Assert.IsType<ForbidResult>(result);
        }

        [Fact]
        public async Task CreateAvailability_InvalidArgument_ReturnsBadRequest()
        {
            // Arrange
            var tutorId = Guid.NewGuid();
            SetupUserClaims(tutorId, "admin");
            var request = new CreateTutorScheduleRequest { TutorId = tutorId };
            _mockTutorScheduleService.Setup(s => s.CreateAvailabilityAsync(request))
                .ThrowsAsync(new ArgumentException("Invalid time range"));

            // Act
            var result = await _controller.CreateAvailability(request);

            // Assert
            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task CreateAvailability_Exception_ReturnsInternalServerError()
        {
            // Arrange
            var tutorId = Guid.NewGuid();
            SetupUserClaims(tutorId, "admin");
            var request = new CreateTutorScheduleRequest { TutorId = tutorId };
            _mockTutorScheduleService.Setup(s => s.CreateAvailabilityAsync(request))
                .ThrowsAsync(new Exception("Database error"));

            // Act
            var result = await _controller.CreateAvailability(request);

            // Assert
            var statusCodeResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(500, statusCodeResult.StatusCode);
        }

        [Fact]
        public async Task UpdateAvailability_ValidRequest_ReturnsOk()
        {
            // Arrange
            var availabilityId = Guid.NewGuid();
            var tutorId = Guid.NewGuid();
            SetupUserClaims(tutorId, "admin");
            var request = new UpdateTutorScheduleRequest();
            _mockTutorScheduleService.Setup(s => s.UpdateAvailabilityAsync(availabilityId, request))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _controller.UpdateAvailability(availabilityId, request);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.NotNull(okResult.Value);
            _mockTutorScheduleService.Verify(s => s.UpdateAvailabilityAsync(availabilityId, request), Times.Once);
        }

        [Fact]
        public async Task UpdateAvailability_NotFound_ReturnsNotFound()
        {
            // Arrange
            var availabilityId = Guid.NewGuid();
            var tutorId = Guid.NewGuid();
            SetupUserClaims(tutorId, "tutor");
            var request = new UpdateTutorScheduleRequest();
            _mockTutorScheduleService.Setup(s => s.GetAvailabilityByIdAsync(availabilityId))
                .ReturnsAsync((TutorScheduleResponse?)null);

            // Act
            var result = await _controller.UpdateAvailability(availabilityId, request);

            // Assert
            Assert.IsType<NotFoundObjectResult>(result);
        }

        [Fact]
        public async Task UpdateAvailability_TutorNotOwner_ReturnsForbid()
        {
            var availabilityId = Guid.NewGuid();
            var tutorId = Guid.NewGuid();
            SetupUserClaims(tutorId, "tutor");
            var request = new UpdateTutorScheduleRequest();
            _mockTutorScheduleService.Setup(s => s.GetAvailabilityByIdAsync(availabilityId))
                .ReturnsAsync(new TutorScheduleResponse { AvailabilityId = availabilityId, TutorId = Guid.NewGuid() });

            var result = await _controller.UpdateAvailability(availabilityId, request);

            Assert.IsType<ForbidResult>(result);
        }

        [Fact]
        public async Task UpdateAvailability_ArgumentException_ReturnsBadRequest()
        {
            var availabilityId = Guid.NewGuid();
            SetupUserClaims(Guid.NewGuid(), "admin");
            _mockTutorScheduleService.Setup(s => s.UpdateAvailabilityAsync(availabilityId, It.IsAny<UpdateTutorScheduleRequest>()))
                .ThrowsAsync(new ArgumentException("bad"));

            var result = await _controller.UpdateAvailability(availabilityId, new UpdateTutorScheduleRequest());

            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task UpdateAvailability_ServerError_Returns500()
        {
            var availabilityId = Guid.NewGuid();
            SetupUserClaims(Guid.NewGuid(), "admin");
            _mockTutorScheduleService.Setup(s => s.UpdateAvailabilityAsync(availabilityId, It.IsAny<UpdateTutorScheduleRequest>()))
                .ThrowsAsync(new Exception("boom"));

            var result = await _controller.UpdateAvailability(availabilityId, new UpdateTutorScheduleRequest());

            var obj = Assert.IsType<ObjectResult>(result);
            Assert.Equal(500, obj.StatusCode);
        }

        [Fact]
        public async Task DeleteAvailability_Success_ReturnsNoContent()
        {
            var availabilityId = Guid.NewGuid();
            SetupUserClaims(Guid.NewGuid(), "admin");
            _mockTutorScheduleService.Setup(s => s.DeleteAvailabilityAsync(availabilityId)).Returns(Task.CompletedTask);

            var result = await _controller.DeleteAvailability(availabilityId);

            Assert.IsType<NoContentResult>(result);
        }

        [Fact]
        public async Task DeleteAvailability_NotFound_ReturnsNotFound()
        {
            var availabilityId = Guid.NewGuid();
            SetupUserClaims(Guid.NewGuid(), "tutor");
            _mockTutorScheduleService.Setup(s => s.GetAvailabilityByIdAsync(availabilityId)).ReturnsAsync((TutorScheduleResponse?)null);

            var result = await _controller.DeleteAvailability(availabilityId);

            Assert.IsType<NotFoundObjectResult>(result);
        }

        [Fact]
        public async Task DeleteAvailability_TutorNotOwner_ReturnsForbid()
        {
            var availabilityId = Guid.NewGuid();
            var tutorId = Guid.NewGuid();
            SetupUserClaims(tutorId, "tutor");
            _mockTutorScheduleService.Setup(s => s.GetAvailabilityByIdAsync(availabilityId)).ReturnsAsync(new TutorScheduleResponse { AvailabilityId = availabilityId, TutorId = Guid.NewGuid() });

            var result = await _controller.DeleteAvailability(availabilityId);

            Assert.IsType<ForbidResult>(result);
        }

        [Fact]
        public async Task DeleteAvailability_ServerError_Returns500()
        {
            var availabilityId = Guid.NewGuid();
            SetupUserClaims(Guid.NewGuid(), "admin");
            _mockTutorScheduleService.Setup(s => s.DeleteAvailabilityAsync(availabilityId)).ThrowsAsync(new Exception("boom"));

            var result = await _controller.DeleteAvailability(availabilityId);

            var obj = Assert.IsType<ObjectResult>(result);
            Assert.Equal(500, obj.StatusCode);
        }

        [Fact]
        public async Task GetAvailabilityById_Found_ReturnsOk()
        {
            var availabilityId = Guid.NewGuid();
            var resp = new TutorScheduleResponse { AvailabilityId = availabilityId };
            _mockTutorScheduleService.Setup(s => s.GetAvailabilityByIdAsync(availabilityId)).ReturnsAsync(resp);

            var result = await _controller.GetAvailabilityById(availabilityId);

            var ok = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(resp, ok.Value);
        }

        [Fact]
        public async Task GetAvailabilityById_NotFound_ReturnsNotFound()
        {
            var availabilityId = Guid.NewGuid();
            _mockTutorScheduleService.Setup(s => s.GetAvailabilityByIdAsync(availabilityId)).ReturnsAsync((TutorScheduleResponse?)null);

            var result = await _controller.GetAvailabilityById(availabilityId);

            Assert.IsType<NotFoundObjectResult>(result);
        }

        [Fact]
        public async Task GetAvailabilityById_ServerError_Returns500()
        {
            var availabilityId = Guid.NewGuid();
            _mockTutorScheduleService.Setup(s => s.GetAvailabilityByIdAsync(availabilityId)).ThrowsAsync(new Exception("boom"));

            var result = await _controller.GetAvailabilityById(availabilityId);

            var obj = Assert.IsType<ObjectResult>(result);
            Assert.Equal(500, obj.StatusCode);
        }

        [Fact]
        public async Task GetTutorSchedules_TutorNotOwner_ReturnsForbid()
        {
            var tutorId = Guid.NewGuid();
            SetupUserClaims(Guid.NewGuid(), "tutor"); // different user

            var result = await _controller.GetTutorSchedules(tutorId);

            Assert.IsType<ForbidResult>(result);
        }

        [Fact]
        public async Task GetTutorSchedules_Admin_ReturnsOk()
        {
            var tutorId = Guid.NewGuid();
            SetupUserClaims(Guid.NewGuid(), "admin");
            var list = new List<TutorScheduleResponse> { new TutorScheduleResponse { AvailabilityId = Guid.NewGuid() } };
            _mockTutorScheduleService.Setup(s => s.GetTutorSchedulesAsync(tutorId, true)).ReturnsAsync(list);

            var result = await _controller.GetTutorSchedules(tutorId);

            var ok = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(list, ok.Value);
        }

        [Fact]
        public async Task GetTutorSchedules_ServerError_Returns500()
        {
            var tutorId = Guid.NewGuid();
            SetupUserClaims(Guid.NewGuid(), "admin");
            _mockTutorScheduleService.Setup(s => s.GetTutorSchedulesAsync(tutorId, true)).ThrowsAsync(new Exception("boom"));

            var result = await _controller.GetTutorSchedules(tutorId);

            var obj = Assert.IsType<ObjectResult>(result);
            Assert.Equal(500, obj.StatusCode);
        }

        [Fact]
        public async Task GetMyAvailabilities_ReturnsOk()
        {
            var tutorId = Guid.NewGuid();
            SetupUserClaims(tutorId, "tutor");
            var list = new List<TutorScheduleResponse> { new TutorScheduleResponse { AvailabilityId = Guid.NewGuid() } };
            _mockTutorScheduleService.Setup(s => s.GetTutorSchedulesAsync(tutorId, true)).ReturnsAsync(list);

            var result = await _controller.GetMyAvailabilities();

            var ok = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(list, ok.Value);
        }

        [Fact]
        public async Task GetMyAvailabilities_ServerError_Returns500()
        {
            var tutorId = Guid.NewGuid();
            SetupUserClaims(tutorId, "tutor");
            _mockTutorScheduleService.Setup(s => s.GetTutorSchedulesAsync(tutorId, true)).ThrowsAsync(new Exception("boom"));

            var result = await _controller.GetMyAvailabilities();

            var obj = Assert.IsType<ObjectResult>(result);
            Assert.Equal(500, obj.StatusCode);
        }

        [Fact]
        public async Task SearchAvailableTutors_ModelInvalid_ReturnsBadRequest()
        {
            _controller.ModelState.AddModelError("StartTime", "Required");

            var result = await _controller.SearchAvailableTutors(new SearchAvailableTutorsRequest());

            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task SearchAvailableTutors_ReturnsOk()
        {
            var req = new SearchAvailableTutorsRequest { Page = 1, PageSize = 10 };
            var list = new List<AvailableTutorResponse> { new AvailableTutorResponse { TutorId = Guid.NewGuid() } };
            _mockTutorScheduleService.Setup(s => s.SearchAvailableTutorsAsync(req)).ReturnsAsync(list);

            var result = await _controller.SearchAvailableTutors(req);

            var ok = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(list, ok.Value);
        }

        [Fact]
        public async Task SearchAvailableTutors_ArgumentException_ReturnsBadRequest()
        {
            var req = new SearchAvailableTutorsRequest { Page = 1, PageSize = 10 };
            _mockTutorScheduleService.Setup(s => s.SearchAvailableTutorsAsync(req)).ThrowsAsync(new ArgumentException("bad"));

            var result = await _controller.SearchAvailableTutors(req);

            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task SearchAvailableTutors_ServerError_Returns500()
        {
            var req = new SearchAvailableTutorsRequest { Page = 1, PageSize = 10 };
            _mockTutorScheduleService.Setup(s => s.SearchAvailableTutorsAsync(req)).ThrowsAsync(new Exception("boom"));

            var result = await _controller.SearchAvailableTutors(req);

            var obj = Assert.IsType<ObjectResult>(result);
            Assert.Equal(500, obj.StatusCode);
        }

        [Fact]
        public async Task UpdateAvailabilityStatus_ModelInvalid_ReturnsBadRequest()
        {
            _controller.ModelState.AddModelError("Status", "Required");

            var result = await _controller.UpdateAvailabilityStatus(Guid.NewGuid(), new UpdateStatusRequest { Status = null! });

            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task UpdateAvailabilityStatus_TutorNotOwner_ReturnsForbid()
        {
            var availabilityId = Guid.NewGuid();
            var tutorId = Guid.NewGuid();
            SetupUserClaims(tutorId, "tutor");
            _mockTutorScheduleService.Setup(s => s.GetAvailabilityByIdAsync(availabilityId)).ReturnsAsync(new TutorScheduleResponse { AvailabilityId = availabilityId, TutorId = Guid.NewGuid() });

            var result = await _controller.UpdateAvailabilityStatus(availabilityId, new UpdateStatusRequest { Status = "active" });

            Assert.IsType<ForbidResult>(result);
        }

        [Fact]
        public async Task UpdateAvailabilityStatus_NotFound_ReturnsNotFound()
        {
            var availabilityId = Guid.NewGuid();
            SetupUserClaims(Guid.NewGuid(), "tutor");
            _mockTutorScheduleService.Setup(s => s.GetAvailabilityByIdAsync(availabilityId)).ReturnsAsync((TutorScheduleResponse?)null);

            var result = await _controller.UpdateAvailabilityStatus(availabilityId, new UpdateStatusRequest { Status = "active" });

            Assert.IsType<NotFoundObjectResult>(result);
        }

        [Fact]
        public async Task UpdateAvailabilityStatus_Success_ReturnsOk()
        {
            var availabilityId = Guid.NewGuid();
            SetupUserClaims(Guid.NewGuid(), "admin");
            _mockTutorScheduleService.Setup(s => s.UpdateAvailabilityStatusAsync(availabilityId, "inactive")).Returns(Task.CompletedTask);

            var result = await _controller.UpdateAvailabilityStatus(availabilityId, new UpdateStatusRequest { Status = "inactive" });

            var ok = Assert.IsType<OkObjectResult>(result);
            Assert.NotNull(ok.Value);
        }

        [Fact]
        public async Task UpdateAvailabilityStatus_ArgumentException_ReturnsBadRequest()
        {
            var availabilityId = Guid.NewGuid();
            SetupUserClaims(Guid.NewGuid(), "admin");
            _mockTutorScheduleService.Setup(s => s.UpdateAvailabilityStatusAsync(availabilityId, It.IsAny<string>())).ThrowsAsync(new ArgumentException("bad"));

            var result = await _controller.UpdateAvailabilityStatus(availabilityId, new UpdateStatusRequest { Status = "inactive" });

            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task UpdateAvailabilityStatus_ServerError_Returns500()
        {
            var availabilityId = Guid.NewGuid();
            SetupUserClaims(Guid.NewGuid(), "admin");
            _mockTutorScheduleService.Setup(s => s.UpdateAvailabilityStatusAsync(availabilityId, It.IsAny<string>())).ThrowsAsync(new Exception("boom"));

            var result = await _controller.UpdateAvailabilityStatus(availabilityId, new UpdateStatusRequest { Status = "inactive" });

            var obj = Assert.IsType<ObjectResult>(result);
            Assert.Equal(500, obj.StatusCode);
        }

        [Fact]
        public async Task BulkCreateAvailabilities_ModelInvalid_ReturnsBadRequest()
        {
            _controller.ModelState.AddModelError("TutorId", "Required");

            var result = await _controller.BulkCreateAvailabilities(new List<CreateTutorScheduleRequest> { new CreateTutorScheduleRequest() });

            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task BulkCreateAvailabilities_TutorNotOwner_ReturnsForbid()
        {
            var tutorId = Guid.NewGuid();
            SetupUserClaims(tutorId, "tutor");
            var requests = new List<CreateTutorScheduleRequest> { new CreateTutorScheduleRequest { TutorId = Guid.NewGuid() } };

            var result = await _controller.BulkCreateAvailabilities(requests);

            Assert.IsType<ForbidResult>(result);
        }

        [Fact]
        public async Task BulkCreateAvailabilities_Success_ReturnsCreated()
        {
            SetupUserClaims(Guid.NewGuid(), "admin");
            var ids = new List<Guid> { Guid.NewGuid(), Guid.NewGuid() };
            _mockTutorScheduleService.Setup(s => s.BulkCreateAvailabilitiesAsync(It.IsAny<List<CreateTutorScheduleRequest>>())).ReturnsAsync(ids);

            var result = await _controller.BulkCreateAvailabilities(new List<CreateTutorScheduleRequest> { new CreateTutorScheduleRequest(), new CreateTutorScheduleRequest() });

            var created = Assert.IsType<CreatedAtActionResult>(result);
            Assert.NotNull(created.Value);
        }

        [Fact]
        public async Task BulkCreateAvailabilities_ArgumentException_ReturnsBadRequest()
        {
            SetupUserClaims(Guid.NewGuid(), "admin");
            _mockTutorScheduleService.Setup(s => s.BulkCreateAvailabilitiesAsync(It.IsAny<List<CreateTutorScheduleRequest>>())).ThrowsAsync(new ArgumentException("bad"));

            var result = await _controller.BulkCreateAvailabilities(new List<CreateTutorScheduleRequest> { new CreateTutorScheduleRequest() });

            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task BulkCreateAvailabilities_ServerError_Returns500()
        {
            SetupUserClaims(Guid.NewGuid(), "admin");
            _mockTutorScheduleService.Setup(s => s.BulkCreateAvailabilitiesAsync(It.IsAny<List<CreateTutorScheduleRequest>>())).ThrowsAsync(new Exception("boom"));

            var result = await _controller.BulkCreateAvailabilities(new List<CreateTutorScheduleRequest> { new CreateTutorScheduleRequest() });

            var obj = Assert.IsType<ObjectResult>(result);
            Assert.Equal(500, obj.StatusCode);
        }

        [Fact]
        public void Constructor_NullTutorScheduleService_ThrowsArgumentNullException()
        {
            // Assert
            Assert.Throws<ArgumentNullException>(() =>
                new TutorScheduleController(null!));
        }

        [Fact]
        public void GetDayFlags_ReturnsOk()
        {
            var result = _controller.GetDayFlags();
            var ok = Assert.IsType<OkObjectResult>(result);
            Assert.NotNull(ok.Value);
        }

        private void SetupUserClaims(Guid userId, string role)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
                new Claim(ClaimTypes.Role, role)
            };
            var identity = new ClaimsIdentity(claims);
            var principal = new ClaimsPrincipal(identity);
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = principal }
            };
        }
    }
}
