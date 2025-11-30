using FluentAssertions;
using MathBridgeSystem.Api.Controllers;
using MathBridgeSystem.Application.DTOs;
using MathBridgeSystem.Application.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Xunit;

namespace MathBridgeSystem.Tests.Controllers
{
    public class SessionControllerTests
    {
        private readonly Mock<ISessionService> _sessionServiceMock;
        private readonly SessionController _controller;
        private readonly Guid _userId = Guid.NewGuid();

        public SessionControllerTests()
        {
            _sessionServiceMock = new Mock<ISessionService>();
            _controller = new SessionController(_sessionServiceMock.Object);
            SetupControllerContext("parent");
        }

        private void SetupControllerContext(string role)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, _userId.ToString()),
                new Claim(ClaimTypes.Role, role)
            };
            var identity = new ClaimsIdentity(claims, "TestAuthType");
            var claimsPrincipal = new ClaimsPrincipal(identity);

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = claimsPrincipal }
            };
        }

        #region Constructor Tests

        [Fact]
        public void Constructor_NullSessionService_ThrowsArgumentNullException()
        {
            Action act = () => new SessionController(null!);
            act.Should().Throw<ArgumentNullException>().WithParameterName("sessionService");
        }

        #endregion

        #region GetSessionsByParent Tests

        [Fact]
        public async Task GetSessionsByParent_ReturnsSessions()
        {
            // Arrange
            var expectedSessions = new List<SessionDto>
            {
                new SessionDto { BookingId = Guid.NewGuid(), SessionDate = DateOnly.FromDateTime(DateTime.Today) },
                new SessionDto { BookingId = Guid.NewGuid(), SessionDate = DateOnly.FromDateTime(DateTime.Today.AddDays(1)) }
            };
            _sessionServiceMock.Setup(s => s.GetSessionsByParentAsync(_userId))
                .ReturnsAsync(expectedSessions);

            // Act
            var result = await _controller.GetSessionsByParent();

            // Assert
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            var sessions = okResult.Value.Should().BeAssignableTo<List<SessionDto>>().Subject;
            sessions.Should().HaveCount(2);
            _sessionServiceMock.Verify(s => s.GetSessionsByParentAsync(_userId), Times.Once);
        }

        [Fact]
        public async Task GetSessionsByParent_EmptyList_ReturnsOkWithEmptyList()
        {
            // Arrange
            _sessionServiceMock.Setup(s => s.GetSessionsByParentAsync(_userId))
                .ReturnsAsync(new List<SessionDto>());

            // Act
            var result = await _controller.GetSessionsByParent();

            // Assert
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            var sessions = okResult.Value.Should().BeAssignableTo<List<SessionDto>>().Subject;
            sessions.Should().BeEmpty();
        }

        [Fact]
        public async Task GetSessionsByParent_ServiceThrowsException_ReturnsBadRequest()
        {
            // Arrange
            _sessionServiceMock.Setup(s => s.GetSessionsByParentAsync(_userId))
                .ThrowsAsync(new Exception("Database error"));

            // Act
            var result = await _controller.GetSessionsByParent();

            // Assert
            result.Should().BeOfType<BadRequestObjectResult>();
        }

        #endregion

        #region GetSessionById Tests

        [Fact]
        public async Task GetSessionById_ExistingSession_ReturnsOkWithSession()
        {
            // Arrange
            var bookingId = Guid.NewGuid();
            var expectedSession = new SessionDto
            {
                BookingId = bookingId,
                SessionDate = DateOnly.FromDateTime(DateTime.Today)
            };
            _sessionServiceMock.Setup(s => s.GetSessionByBookingIdAsync(bookingId, _userId, "parent"))
                .ReturnsAsync(expectedSession);

            // Act
            var result = await _controller.GetSessionById(bookingId);

            // Assert
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            var session = okResult.Value.Should().BeAssignableTo<SessionDto>().Subject;
            session.BookingId.Should().Be(bookingId);
            _sessionServiceMock.Verify(s => s.GetSessionByBookingIdAsync(bookingId, _userId, "parent"), Times.Once);
        }

        [Fact]
        public async Task GetSessionById_NonExistingSession_ReturnsNotFound()
        {
            // Arrange
            var bookingId = Guid.NewGuid();
            _sessionServiceMock.Setup(s => s.GetSessionByBookingIdAsync(bookingId, _userId, "parent"))
                .ReturnsAsync((SessionDto)null!);

            // Act
            var result = await _controller.GetSessionById(bookingId);

            // Assert
            result.Should().BeOfType<NotFoundResult>();
        }

        [Fact]
        public async Task GetSessionById_ServiceThrowsException_ReturnsBadRequest()
        {
            // Arrange
            var bookingId = Guid.NewGuid();
            _sessionServiceMock.Setup(s => s.GetSessionByBookingIdAsync(bookingId, _userId, "parent"))
                .ThrowsAsync(new Exception("Unauthorized access"));

            // Act
            var result = await _controller.GetSessionById(bookingId);

            // Assert
            result.Should().BeOfType<BadRequestObjectResult>();
        }

        [Fact]
        public async Task GetSessionById_TutorRole_CallsServiceWithTutorRole()
        {
            // Arrange
            SetupControllerContext("tutor");
            var bookingId = Guid.NewGuid();
            var expectedSession = new SessionDto { BookingId = bookingId };
            _sessionServiceMock.Setup(s => s.GetSessionByBookingIdAsync(bookingId, _userId, "tutor"))
                .ReturnsAsync(expectedSession);

            // Act
            var result = await _controller.GetSessionById(bookingId);

            // Assert
            result.Should().BeOfType<OkObjectResult>();
            _sessionServiceMock.Verify(s => s.GetSessionByBookingIdAsync(bookingId, _userId, "tutor"), Times.Once);
        }

        [Fact]
        public async Task GetSessionById_StaffRole_CallsServiceWithStaffRole()
        {
            // Arrange
            SetupControllerContext("staff");
            var bookingId = Guid.NewGuid();
            var expectedSession = new SessionDto { BookingId = bookingId };
            _sessionServiceMock.Setup(s => s.GetSessionByBookingIdAsync(bookingId, _userId, "staff"))
                .ReturnsAsync(expectedSession);

            // Act
            var result = await _controller.GetSessionById(bookingId);

            // Assert
            result.Should().BeOfType<OkObjectResult>();
            _sessionServiceMock.Verify(s => s.GetSessionByBookingIdAsync(bookingId, _userId, "staff"), Times.Once);
        }

        #endregion

        #region GetSessionsByChildId Tests

        [Fact]
        public async Task GetSessionsByChildId_ReturnsSessions()
        {
            // Arrange
            var childId = Guid.NewGuid();
            var expectedSessions = new List<SessionDto>
            {
                new SessionDto { BookingId = Guid.NewGuid(), SessionDate = DateOnly.FromDateTime(DateTime.Today) },
                new SessionDto { BookingId = Guid.NewGuid(), SessionDate = DateOnly.FromDateTime(DateTime.Today.AddDays(1)) }
            };
            _sessionServiceMock.Setup(s => s.GetSessionsByChildIdAsync(childId))
                .ReturnsAsync(expectedSessions);

            // Act
            var result = await _controller.GetSessionsByChildId(childId);

            // Assert
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            var sessions = okResult.Value.Should().BeAssignableTo<List<SessionDto>>().Subject;
            sessions.Should().HaveCount(2);
            _sessionServiceMock.Verify(s => s.GetSessionsByChildIdAsync(childId), Times.Once);
        }

        [Fact]
        public async Task GetSessionsByChildId_EmptyList_ReturnsOkWithEmptyList()
        {
            // Arrange
            var childId = Guid.NewGuid();
            _sessionServiceMock.Setup(s => s.GetSessionsByChildIdAsync(childId))
                .ReturnsAsync(new List<SessionDto>());

            // Act
            var result = await _controller.GetSessionsByChildId(childId);

            // Assert
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            var sessions = okResult.Value.Should().BeAssignableTo<List<SessionDto>>().Subject;
            sessions.Should().BeEmpty();
        }

        [Fact]
        public async Task GetSessionsByChildId_ServiceThrowsException_ReturnsBadRequest()
        {
            // Arrange
            var childId = Guid.NewGuid();
            _sessionServiceMock.Setup(s => s.GetSessionsByChildIdAsync(childId))
                .ThrowsAsync(new Exception("Child not found"));

            // Act
            var result = await _controller.GetSessionsByChildId(childId);

            // Assert
            result.Should().BeOfType<BadRequestObjectResult>();
        }

        #endregion

        #region GetSessionsByTutor Tests

        [Fact]
        public async Task GetSessionsByTutor_StaffWithoutTutorId_ReturnsBadRequest()
        {
            // Arrange - staff role
            SetupControllerContext("staff");

            // Act
            var result = await _controller.GetSessionsByTutor(null);

            // Assert
            var bad = result.Should().BeOfType<BadRequestObjectResult>().Subject;
            bad.Value.Should().NotBeNull();
        }

        [Fact]
        public async Task GetSessionsByTutor_StaffWithTutorId_ReturnsOk()
        {
            // Arrange
            SetupControllerContext("staff");
            var tutorId = Guid.NewGuid();
            var sessions = new List<SessionDto> { new SessionDto { BookingId = Guid.NewGuid() } };
            _sessionServiceMock.Setup(s => s.GetSessionsByTutorIdAsync(tutorId)).ReturnsAsync(sessions);

            // Act
            var result = await _controller.GetSessionsByTutor(tutorId);

            // Assert
            var ok = result.Should().BeOfType<OkObjectResult>().Subject;
            ok.Value.Should().NotBeNull();
            _sessionServiceMock.Verify(s => s.GetSessionsByTutorIdAsync(tutorId), Times.Once);
        }

        [Fact]
        public async Task GetSessionsByTutor_TutorWithDifferentTutorId_ReturnsForbid()
        {
            // Arrange
            SetupControllerContext("tutor");
            var other = Guid.NewGuid();

            // Act
            var result = await _controller.GetSessionsByTutor(other);

            // Assert
            result.Should().BeOfType<ForbidResult>();
        }

        [Fact]
        public async Task GetSessionsByTutor_TutorWithoutTutorId_UsesCurrentUser_ReturnsOk()
        {
            // Arrange
            SetupControllerContext("tutor");
            var sessions = new List<SessionDto> { new SessionDto { BookingId = Guid.NewGuid() } };
            _sessionServiceMock.Setup(s => s.GetSessionsByTutorIdAsync(_userId)).ReturnsAsync(sessions);

            // Act
            var result = await _controller.GetSessionsByTutor(null);

            // Assert
            var ok = result.Should().BeOfType<OkObjectResult>().Subject;
            ok.Value.Should().NotBeNull();
            _sessionServiceMock.Verify(s => s.GetSessionsByTutorIdAsync(_userId), Times.Once);
        }

        [Fact]
        public async Task GetSessionsByTutor_ServiceThrowsException_ReturnsBadRequest()
        {
            // Arrange
            SetupControllerContext("tutor");
            _sessionServiceMock.Setup(s => s.GetSessionsByTutorIdAsync(_userId)).ThrowsAsync(new Exception("boom"));

            // Act
            var result = await _controller.GetSessionsByTutor(null);

            // Assert
            result.Should().BeOfType<BadRequestObjectResult>();
        }

        #endregion

        #region UpdateSessionTutor Tests

        [Fact]
        public async Task UpdateSessionTutor_ModelStateInvalid_ReturnsBadRequest()
        {
            // Arrange
            _controller.ModelState.AddModelError("NewTutorId", "Required");

            // Act
            var result = await _controller.UpdateSessionTutor(Guid.NewGuid(), new UpdateSessionTutorRequest());

            // Assert
            result.Should().BeOfType<BadRequestObjectResult>();
        }

        [Fact]
        public async Task UpdateSessionTutor_Success_ReturnsOk()
        {
            // Arrange
            SetupControllerContext("staff");
            var bookingId = Guid.NewGuid();
            var newTutor = Guid.NewGuid();
            _sessionServiceMock.Setup(s => s.UpdateSessionTutorAsync(bookingId, newTutor, _userId)).ReturnsAsync(true);

            // Act
            var result = await _controller.UpdateSessionTutor(bookingId, new UpdateSessionTutorRequest { NewTutorId = newTutor });

            // Assert
            var ok = result.Should().BeOfType<OkObjectResult>().Subject;
            ok.Value.Should().NotBeNull();
            _sessionServiceMock.Verify(s => s.UpdateSessionTutorAsync(bookingId, newTutor, _userId), Times.Once);
        }

        [Fact]
        public async Task UpdateSessionTutor_NotFound_ReturnsNotFound()
        {
            SetupControllerContext("staff");
            var bookingId = Guid.NewGuid();
            _sessionServiceMock.Setup(s => s.UpdateSessionTutorAsync(bookingId, It.IsAny<Guid>(), _userId))
                .ThrowsAsync(new KeyNotFoundException("not found"));

            var result = await _controller.UpdateSessionTutor(bookingId, new UpdateSessionTutorRequest { NewTutorId = Guid.NewGuid() });

            result.Should().BeOfType<NotFoundObjectResult>();
        }

        [Fact]
        public async Task UpdateSessionTutor_Unauthorized_ReturnsForbid()
        {
            SetupControllerContext("staff");
            var bookingId = Guid.NewGuid();
            _sessionServiceMock.Setup(s => s.UpdateSessionTutorAsync(bookingId, It.IsAny<Guid>(), _userId))
                .ThrowsAsync(new UnauthorizedAccessException("no"));

            var result = await _controller.UpdateSessionTutor(bookingId, new UpdateSessionTutorRequest { NewTutorId = Guid.NewGuid() });

            result.Should().BeOfType<ForbidResult>();
        }

        [Fact]
        public async Task UpdateSessionTutor_InvalidOperation_ReturnsBadRequest()
        {
            SetupControllerContext("staff");
            var bookingId = Guid.NewGuid();
            _sessionServiceMock.Setup(s => s.UpdateSessionTutorAsync(bookingId, It.IsAny<Guid>(), _userId))
                .ThrowsAsync(new InvalidOperationException("bad"));

            var result = await _controller.UpdateSessionTutor(bookingId, new UpdateSessionTutorRequest { NewTutorId = Guid.NewGuid() });

            result.Should().BeOfType<BadRequestObjectResult>();
        }

        [Fact]
        public async Task UpdateSessionTutor_ArgumentException_ReturnsBadRequest()
        {
            SetupControllerContext("staff");
            var bookingId = Guid.NewGuid();
            _sessionServiceMock.Setup(s => s.UpdateSessionTutorAsync(bookingId, It.IsAny<Guid>(), _userId))
                .ThrowsAsync(new ArgumentException("arg"));

            var result = await _controller.UpdateSessionTutor(bookingId, new UpdateSessionTutorRequest { NewTutorId = Guid.NewGuid() });

            result.Should().BeOfType<BadRequestObjectResult>();
        }

        [Fact]
        public async Task UpdateSessionTutor_ServerError_Returns500()
        {
            SetupControllerContext("staff");
            var bookingId = Guid.NewGuid();
            _sessionServiceMock.Setup(s => s.UpdateSessionTutorAsync(bookingId, It.IsAny<Guid>(), _userId))
                .ThrowsAsync(new Exception("boom"));

            var result = await _controller.UpdateSessionTutor(bookingId, new UpdateSessionTutorRequest { NewTutorId = Guid.NewGuid() });

            var obj = result.Should().BeOfType<ObjectResult>().Subject;
            obj.StatusCode.Should().Be(500);
        }

        #endregion

        #region UpdateSessionStatus Tests

        [Fact]
        public async Task UpdateSessionStatus_ModelStateInvalid_ReturnsBadRequest()
        {
            _controller.ModelState.AddModelError("Status", "Required");

            var result = await _controller.UpdateSessionStatus(Guid.NewGuid(), new UpdateSessionStatusRequest());

            result.Should().BeOfType<BadRequestObjectResult>();
        }

        [Fact]
        public async Task UpdateSessionStatus_NonStaffWithoutSession_Forbid()
        {
            // Arrange as tutor
            SetupControllerContext("tutor");
            var bookingId = Guid.NewGuid();
            _sessionServiceMock.Setup(s => s.GetSessionForTutorCheckAsync(bookingId, _userId)).ReturnsAsync((SessionDto)null!);

            // Act
            var result = await _controller.UpdateSessionStatus(bookingId, new UpdateSessionStatusRequest { Status = "done" });

            // Assert
            result.Should().BeOfType<ForbidResult>();
        }

        [Fact]
        public async Task UpdateSessionStatus_Staff_Success_ReturnsOk()
        {
            SetupControllerContext("staff");
            var bookingId = Guid.NewGuid();
            _sessionServiceMock.Setup(s => s.UpdateSessionStatusAsync(bookingId, "done", _userId)).ReturnsAsync(true);

            var result = await _controller.UpdateSessionStatus(bookingId, new UpdateSessionStatusRequest { Status = "done" });

            var ok = result.Should().BeOfType<OkObjectResult>().Subject;
            ok.Value.Should().NotBeNull();
            _sessionServiceMock.Verify(s => s.UpdateSessionStatusAsync(bookingId, "done", _userId), Times.Once);
        }

        [Fact]
        public async Task UpdateSessionStatus_Tutor_Success_ReturnsOk()
        {
            SetupControllerContext("tutor");
            var bookingId = Guid.NewGuid();
            _sessionServiceMock.Setup(s => s.GetSessionForTutorCheckAsync(bookingId, _userId)).ReturnsAsync(new SessionDto { BookingId = bookingId });
            _sessionServiceMock.Setup(s => s.UpdateSessionStatusAsync(bookingId, "done", _userId)).ReturnsAsync(true);

            var result = await _controller.UpdateSessionStatus(bookingId, new UpdateSessionStatusRequest { Status = "done" });

            var ok = result.Should().BeOfType<OkObjectResult>().Subject;
            ok.Value.Should().NotBeNull();
        }

        [Fact]
        public async Task UpdateSessionStatus_NotFound_ReturnsNotFound()
        {
            SetupControllerContext("tutor");
            var bookingId = Guid.NewGuid();
            _sessionServiceMock.Setup(s => s.GetSessionForTutorCheckAsync(bookingId, _userId)).ReturnsAsync(new SessionDto { BookingId = bookingId });
            _sessionServiceMock.Setup(s => s.UpdateSessionStatusAsync(bookingId, "done", _userId)).ThrowsAsync(new KeyNotFoundException());

            var result = await _controller.UpdateSessionStatus(bookingId, new UpdateSessionStatusRequest { Status = "done" });

            result.Should().BeOfType<NotFoundObjectResult>();
        }

        [Fact]
        public async Task UpdateSessionStatus_Unauthorized_ReturnsForbid()
        {
            SetupControllerContext("tutor");
            var bookingId = Guid.NewGuid();
            _sessionServiceMock.Setup(s => s.GetSessionForTutorCheckAsync(bookingId, _userId)).ReturnsAsync(new SessionDto { BookingId = bookingId });
            _sessionServiceMock.Setup(s => s.UpdateSessionStatusAsync(bookingId, "done", _userId)).ThrowsAsync(new UnauthorizedAccessException("no"));

            var result = await _controller.UpdateSessionStatus(bookingId, new UpdateSessionStatusRequest { Status = "done" });

            result.Should().BeOfType<ForbidResult>();
        }

        [Fact]
        public async Task UpdateSessionStatus_InvalidOperation_ReturnsBadRequest()
        {
            SetupControllerContext("tutor");
            var bookingId = Guid.NewGuid();
            _sessionServiceMock.Setup(s => s.GetSessionForTutorCheckAsync(bookingId, _userId)).ReturnsAsync(new SessionDto { BookingId = bookingId });
            _sessionServiceMock.Setup(s => s.UpdateSessionStatusAsync(bookingId, "done", _userId)).ThrowsAsync(new InvalidOperationException("bad"));

            var result = await _controller.UpdateSessionStatus(bookingId, new UpdateSessionStatusRequest { Status = "done" });

            result.Should().BeOfType<BadRequestObjectResult>();
        }

        [Fact]
        public async Task UpdateSessionStatus_ArgumentException_ReturnsBadRequest()
        {
            SetupControllerContext("tutor");
            var bookingId = Guid.NewGuid();
            _sessionServiceMock.Setup(s => s.GetSessionForTutorCheckAsync(bookingId, _userId)).ReturnsAsync(new SessionDto { BookingId = bookingId });
            _sessionServiceMock.Setup(s => s.UpdateSessionStatusAsync(bookingId, "done", _userId)).ThrowsAsync(new ArgumentException("bad"));

            var result = await _controller.UpdateSessionStatus(bookingId, new UpdateSessionStatusRequest { Status = "done" });

            result.Should().BeOfType<BadRequestObjectResult>();
        }

        [Fact]
        public async Task UpdateSessionStatus_ServerError_Returns500()
        {
            SetupControllerContext("tutor");
            var bookingId = Guid.NewGuid();
            _sessionServiceMock.Setup(s => s.GetSessionForTutorCheckAsync(bookingId, _userId)).ReturnsAsync(new SessionDto { BookingId = bookingId });
            _sessionServiceMock.Setup(s => s.UpdateSessionStatusAsync(bookingId, "done", _userId)).ThrowsAsync(new Exception("boom"));

            var result = await _controller.UpdateSessionStatus(bookingId, new UpdateSessionStatusRequest { Status = "done" });

            var obj = result.Should().BeOfType<ObjectResult>().Subject;
            obj.StatusCode.Should().Be(500);
        }

        #endregion
    }
}

