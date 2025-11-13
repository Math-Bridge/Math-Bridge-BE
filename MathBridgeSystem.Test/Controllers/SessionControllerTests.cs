using FluentAssertions;
using MathBridgeSystem.Api.Controllers;
using MathBridgeSystem.Application.DTOs;
using MathBridgeSystem.Application.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using System.Security.Claims;
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
            _sessionServiceMock.Setup(s => s.GetSessionsByChildIdAsync(childId, _userId))
                .ReturnsAsync(expectedSessions);

            // Act
            var result = await _controller.GetSessionsByChildId(childId);

            // Assert
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            var sessions = okResult.Value.Should().BeAssignableTo<List<SessionDto>>().Subject;
            sessions.Should().HaveCount(2);
            _sessionServiceMock.Verify(s => s.GetSessionsByChildIdAsync(childId, _userId), Times.Once);
        }

        [Fact]
        public async Task GetSessionsByChildId_EmptyList_ReturnsOkWithEmptyList()
        {
            // Arrange
            var childId = Guid.NewGuid();
            _sessionServiceMock.Setup(s => s.GetSessionsByChildIdAsync(childId, _userId))
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
            _sessionServiceMock.Setup(s => s.GetSessionsByChildIdAsync(childId, _userId))
                .ThrowsAsync(new Exception("Child not found"));

            // Act
            var result = await _controller.GetSessionsByChildId(childId);

            // Assert
            result.Should().BeOfType<BadRequestObjectResult>();
        }

        #endregion
    }
}

