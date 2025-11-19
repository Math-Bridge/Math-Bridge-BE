using MathBridgeSystem.Api.Controllers;
using MathBridgeSystem.Application.DTOs.VideoConference;
using MathBridgeSystem.Application.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using System.Security.Claims;
using Xunit;
using Assert = Xunit.Assert;

namespace MathBridgeSystem.Tests.Controllers
{
    public class VideoConferenceControllerTests
    {
        private readonly Mock<IVideoConferenceService> _mockVideoConferenceService;
        private readonly VideoConferenceController _controller;
        private readonly Guid _userId = Guid.NewGuid();

        public VideoConferenceControllerTests()
        {
            _mockVideoConferenceService = new Mock<IVideoConferenceService>();
            _controller = new VideoConferenceController(_mockVideoConferenceService.Object);

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
        public async Task CreateVideoConference_ValidRequest_ReturnsOk()
        {
            // Arrange
            var request = new CreateVideoConferenceRequest();
            var conference = new VideoConferenceSessionDto
            {
                ConferenceId = Guid.NewGuid(),
                BookingId = Guid.NewGuid(),
                Platform = "Zoom"
            };
            _mockVideoConferenceService.Setup(s => s.CreateVideoConferenceAsync(request, _userId))
                .ReturnsAsync(conference);

            // Act
            var result = await _controller.CreateVideoConference(request);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.NotNull(okResult.Value);
            _mockVideoConferenceService.Verify(s => s.CreateVideoConferenceAsync(request, _userId), Times.Once);
        }
        

        [Fact]
        public async Task CreateVideoConference_Exception_ReturnsInternalServerError()
        {
            // Arrange
            var request = new CreateVideoConferenceRequest();
            _mockVideoConferenceService.Setup(s => s.CreateVideoConferenceAsync(request, _userId))
                .ThrowsAsync(new Exception("Service error"));

            // Act
            var result = await _controller.CreateVideoConference(request);

            // Assert
            var objectResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(500, objectResult.StatusCode);
        }

        [Fact]
        public async Task GetVideoConference_ValidId_ReturnsOk()
        {
            // Arrange
            var conferenceId = Guid.NewGuid();
            var conference = new VideoConferenceSessionDto { ConferenceId = conferenceId };
            _mockVideoConferenceService.Setup(s => s.GetVideoConferenceAsync(conferenceId))
                .ReturnsAsync(conference);

            // Act
            var result = await _controller.GetVideoConference(conferenceId);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.NotNull(okResult.Value);
            _mockVideoConferenceService.Verify(s => s.GetVideoConferenceAsync(conferenceId), Times.Once);
        }

        [Fact]
        public async Task GetVideoConference_NotFound_ReturnsNotFound()
        {
            // Arrange
            var conferenceId = Guid.NewGuid();
            _mockVideoConferenceService.Setup(s => s.GetVideoConferenceAsync(conferenceId))
                .ThrowsAsync(new KeyNotFoundException("Conference not found"));

            // Act
            var result = await _controller.GetVideoConference(conferenceId);

            // Assert
            Assert.IsType<NotFoundObjectResult>(result);
        }

        [Fact]
        public async Task GetVideoConferencesByBooking_ValidId_ReturnsOk()
        {
            // Arrange
            var bookingId = Guid.NewGuid();
            var conferences = new List<VideoConferenceSessionDto>
            {
                new VideoConferenceSessionDto { ConferenceId = Guid.NewGuid(), BookingId = bookingId }
            };
            _mockVideoConferenceService.Setup(s => s.GetVideoConferencesByBookingAsync(bookingId))
                .ReturnsAsync(conferences);

            // Act
            var result = await _controller.GetVideoConferencesByBooking(bookingId);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.NotNull(okResult.Value);
            _mockVideoConferenceService.Verify(s => s.GetVideoConferencesByBookingAsync(bookingId), Times.Once);
        }

        [Fact]
        public async Task GetVideoConferencesByContract_ValidId_ReturnsOk()
        {
            // Arrange
            var contractId = Guid.NewGuid();
            var conferences = new List<VideoConferenceSessionDto>
            {
                new VideoConferenceSessionDto { ConferenceId = Guid.NewGuid(), ContractId = contractId }
            };
            _mockVideoConferenceService.Setup(s => s.GetVideoConferencesByContractAsync(contractId))
                .ReturnsAsync(conferences);

            // Act
            var result = await _controller.GetVideoConferencesByContract(contractId);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.NotNull(okResult.Value);
            _mockVideoConferenceService.Verify(s => s.GetVideoConferencesByContractAsync(contractId), Times.Once);
        }

        [Fact]
        public void Constructor_NullVideoConferenceService_ThrowsArgumentNullException()
        {
            // Assert
            Assert.Throws<ArgumentNullException>(() =>
                new VideoConferenceController(null!));
        }
    }
}
