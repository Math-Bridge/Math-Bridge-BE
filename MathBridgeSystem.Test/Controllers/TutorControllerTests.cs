using FluentAssertions;
using MathBridgeSystem.Application.DTOs;
using MathBridgeSystem.Application.Interfaces;
using MathBridgeSystem.Api.Controllers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using System.Security.Claims;
using Xunit;

namespace MathBridgeSystem.Tests.Controllers
{
    public class TutorControllerTests
    {
        private readonly Mock<ITutorService> _tutorServiceMock;
        private readonly TutorsController _controller;
        private readonly Guid _testUserId;

        public TutorControllerTests()
        {
            _tutorServiceMock = new Mock<ITutorService>();
            _controller = new TutorsController(_tutorServiceMock.Object);
            _testUserId = Guid.NewGuid();

            // Setup user claims
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, _testUserId.ToString()),
                new Claim(ClaimTypes.Role, "tutor")
            };
            var identity = new ClaimsIdentity(claims, "Test");
            var claimsPrincipal = new ClaimsPrincipal(identity);
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = claimsPrincipal }
            };
        }

        [Fact]
        public void Constructor_NullTutorService_ThrowsArgumentNullException()
        {
            // Act & Assert
            Action act = () => new TutorsController(null!);
            act.Should().Throw<ArgumentNullException>();
        }

        [Fact]
        public async Task GetTutor_ValidId_ReturnsOkWithTutor()
        {
            // Arrange
            var tutorId = Guid.NewGuid();
            var tutorDto = new TutorDto
            {
                UserId = tutorId,
                FullName = "John Tutor",
                Email = "john@test.com"
            };

            _tutorServiceMock.Setup(s => s.GetTutorByIdAsync(tutorId, _testUserId, "tutor"))
                .ReturnsAsync(tutorDto);

            // Act
            var result = await _controller.GetTutor(tutorId);

            // Assert
            var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
            okResult.Value.Should().BeEquivalentTo(tutorDto);
        }

        [Fact]
        public async Task GetTutor_ServiceThrowsException_ReturnsBadRequest()
        {
            // Arrange
            var tutorId = Guid.NewGuid();
            _tutorServiceMock.Setup(s => s.GetTutorByIdAsync(tutorId, _testUserId, "tutor"))
                .ThrowsAsync(new Exception("Tutor not found"));

            // Act
            var result = await _controller.GetTutor(tutorId);

            // Assert
            result.Result.Should().BeOfType<BadRequestObjectResult>();
        }

        [Fact]
        public async Task UpdateTutor_ValidRequest_ReturnsOkWithMessage()
        {
            // Arrange
            var tutorId = Guid.NewGuid();
            var request = new UpdateTutorRequest
            {
                FullName = "Updated Name",
                PhoneNumber = "1234567890"
            };

            _tutorServiceMock.Setup(s => s.UpdateTutorAsync(tutorId, request, _testUserId, "tutor"))
                .ReturnsAsync(tutorId);

            // Act
            var result = await _controller.UpdateTutor(tutorId, request);

            // Assert
            var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
            okResult.Value.Should().NotBeNull();
        }

        [Fact]
        public async Task UpdateTutor_NullRequest_ReturnsBadRequest()
        {
            // Arrange
            var tutorId = Guid.NewGuid();

            // Act
            var result = await _controller.UpdateTutor(tutorId, null!);

            // Assert
            result.Result.Should().BeOfType<BadRequestObjectResult>();
        }

        [Fact]
        public async Task UpdateTutor_UnauthorizedAccess_ReturnsUnauthorized()
        {
            // Arrange
            var tutorId = Guid.NewGuid();
            var request = new UpdateTutorRequest { FullName = "Test" };

            _tutorServiceMock.Setup(s => s.UpdateTutorAsync(tutorId, request, _testUserId, "tutor"))
                .ThrowsAsync(new UnauthorizedAccessException("Not authorized"));

            // Act
            var result = await _controller.UpdateTutor(tutorId, request);

            // Assert
            result.Result.Should().BeOfType<UnauthorizedObjectResult>();
        }

        [Fact]
        public async Task GetAllTutors_ReturnsOkWithList()
        {
            // Arrange
            var tutors = new List<TutorDto>
            {
                new TutorDto { UserId = Guid.NewGuid(), FullName = "Tutor 1" },
                new TutorDto { UserId = Guid.NewGuid(), FullName = "Tutor 2" }
            };

            _tutorServiceMock.Setup(s => s.GetAllTutorsAsync())
                .ReturnsAsync(tutors);

            // Act
            var result = await _controller.GetAllTutors();

            // Assert
            var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
            var returnedTutors = okResult.Value.Should().BeAssignableTo<List<TutorDto>>().Subject;
            returnedTutors.Should().HaveCount(2);
        }

        [Fact]
        public async Task GetAllTutors_ServiceThrowsException_ReturnsBadRequest()
        {
            // Arrange
            _tutorServiceMock.Setup(s => s.GetAllTutorsAsync())
                .ThrowsAsync(new Exception("Database error"));

            // Act
            var result = await _controller.GetAllTutors();

            // Assert
            result.Result.Should().BeOfType<BadRequestObjectResult>();
        }
    }
}
