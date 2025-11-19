using MathBridgeSystem.Api.Controllers;
using MathBridgeSystem.Application.DTOs.TutorVerification;
using MathBridgeSystem.Application.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using System.Security.Claims;
using Xunit;
using Assert = Xunit.Assert;

namespace MathBridgeSystem.Tests.Controllers
{
    public class TutorVerificationControllerTests
    {
        private readonly Mock<ITutorVerificationService> _mockVerificationService;
        private readonly TutorVerificationController _controller;

        public TutorVerificationControllerTests()
        {
            _mockVerificationService = new Mock<ITutorVerificationService>();
            _controller = new TutorVerificationController(_mockVerificationService.Object);
        }

        [Fact]
        public async Task CreateVerification_ValidRequest_ReturnsCreatedResult()
        {
            // Arrange
            var userId = Guid.NewGuid();
            SetupUserClaims(userId, "tutor");
            var request = new CreateTutorVerificationRequest { UserId = userId, University = "U", Major = "M", HourlyRate = 10 };
            var verificationId = Guid.NewGuid();
            _mockVerificationService.Setup(s => s.CreateVerificationAsync(request))
                .ReturnsAsync(verificationId);

            // Act
            var result = await _controller.CreateVerification(request);

            // Assert
            var createdResult = Assert.IsType<CreatedAtActionResult>(result);
            Assert.Equal(nameof(_controller.GetVerificationById), createdResult.ActionName);
            _mockVerificationService.Verify(s => s.CreateVerificationAsync(request), Times.Once);
        }

        [Fact]
        public async Task CreateVerification_TutorNotOwner_ReturnsForbid()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var differentUserId = Guid.NewGuid();
            SetupUserClaims(userId, "tutor");
            var request = new CreateTutorVerificationRequest { UserId = differentUserId };

            // Act
            var result = await _controller.CreateVerification(request);

            // Assert
            Assert.IsType<ForbidResult>(result);
        }

        [Fact]
        public async Task CreateVerification_AlreadyExists_ReturnsConflict()
        {
            // Arrange
            var userId = Guid.NewGuid();
            SetupUserClaims(userId, "admin");
            var request = new CreateTutorVerificationRequest { UserId = userId };
            _mockVerificationService.Setup(s => s.CreateVerificationAsync(request))
                .ThrowsAsync(new ArgumentException("Verification already exists"));

            // Act
            var result = await _controller.CreateVerification(request);

            // Assert
            Assert.IsType<ConflictObjectResult>(result);
        }

        [Fact]
        public async Task CreateVerification_UserNotFound_ReturnsNotFound()
        {
            // Arrange
            var userId = Guid.NewGuid();
            SetupUserClaims(userId, "admin");
            var request = new CreateTutorVerificationRequest { UserId = userId };
            _mockVerificationService.Setup(s => s.CreateVerificationAsync(request))
                .ThrowsAsync(new KeyNotFoundException("User not found"));

            // Act
            var result = await _controller.CreateVerification(request);

            // Assert
            Assert.IsType<NotFoundObjectResult>(result);
        }

        [Fact]
        public async Task CreateVerification_InvalidArgument_ReturnsBadRequest()
        {
            // Arrange
            var userId = Guid.NewGuid();
            SetupUserClaims(userId, "admin");
            var request = new CreateTutorVerificationRequest { UserId = userId };
            _mockVerificationService.Setup(s => s.CreateVerificationAsync(request))
                .ThrowsAsync(new ArgumentException("Invalid data"));

            // Act
            var result = await _controller.CreateVerification(request);

            // Assert
            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task CreateVerification_Exception_ReturnsInternalServerError()
        {
            // Arrange
            var userId = Guid.NewGuid();
            SetupUserClaims(userId, "admin");
            var request = new CreateTutorVerificationRequest { UserId = userId };
            _mockVerificationService.Setup(s => s.CreateVerificationAsync(request))
                .ThrowsAsync(new Exception("Database error"));

            // Act
            var result = await _controller.CreateVerification(request);

            // Assert
            var statusCodeResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(500, statusCodeResult.StatusCode);
        }

        // ----- UpdateVerification tests -----
        [Fact]
        public async Task UpdateVerification_Success_ReturnsOk()
        {
            var id = Guid.NewGuid();
            var userId = Guid.NewGuid();
            SetupUserClaims(userId, "admin");
            var existing = new TutorVerificationDto { VerificationId = id, UserId = userId };
            _mockVerificationService.Setup(s => s.GetVerificationByIdAsync(id)).ReturnsAsync(existing);
            _mockVerificationService.Setup(s => s.UpdateVerificationAsync(id, It.IsAny<UpdateTutorVerificationRequest>())).Returns(Task.CompletedTask);

            var result = await _controller.UpdateVerification(id, new UpdateTutorVerificationRequest());

            var ok = Assert.IsType<OkObjectResult>(result);
            Assert.NotNull(ok.Value);
        }

        [Fact]
        public async Task UpdateVerification_NotFound_ReturnsNotFound()
        {
            var id = Guid.NewGuid();
            _mockVerificationService.Setup(s => s.GetVerificationByIdAsync(id)).ReturnsAsync((TutorVerificationDto?)null);

            var result = await _controller.UpdateVerification(id, new UpdateTutorVerificationRequest());

            Assert.IsType<NotFoundObjectResult>(result);
        }

        [Fact]
        public async Task UpdateVerification_TutorNotOwner_ReturnsForbid()
        {
            var id = Guid.NewGuid();
            var userId = Guid.NewGuid();
            SetupUserClaims(userId, "tutor");
            _mockVerificationService.Setup(s => s.GetVerificationByIdAsync(id)).ReturnsAsync(new TutorVerificationDto { VerificationId = id, UserId = Guid.NewGuid() });

            var result = await _controller.UpdateVerification(id, new UpdateTutorVerificationRequest());

            Assert.IsType<ForbidResult>(result);
        }

        [Fact]
        public async Task UpdateVerification_ArgumentException_ReturnsBadRequest()
        {
            var id = Guid.NewGuid();
            var userId = Guid.NewGuid();
            SetupUserClaims(userId, "admin");
            _mockVerificationService.Setup(s => s.GetVerificationByIdAsync(id)).ReturnsAsync(new TutorVerificationDto { VerificationId = id, UserId = userId });
            _mockVerificationService.Setup(s => s.UpdateVerificationAsync(id, It.IsAny<UpdateTutorVerificationRequest>())).ThrowsAsync(new ArgumentException("bad"));

            var result = await _controller.UpdateVerification(id, new UpdateTutorVerificationRequest());

            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task UpdateVerification_Exception_Returns500()
        {
            var id = Guid.NewGuid();
            var userId = Guid.NewGuid();
            SetupUserClaims(userId, "admin");
            _mockVerificationService.Setup(s => s.GetVerificationByIdAsync(id)).ReturnsAsync(new TutorVerificationDto { VerificationId = id, UserId = userId });
            _mockVerificationService.Setup(s => s.UpdateVerificationAsync(id, It.IsAny<UpdateTutorVerificationRequest>())).ThrowsAsync(new Exception("boom"));

            var result = await _controller.UpdateVerification(id, new UpdateTutorVerificationRequest());

            var obj = Assert.IsType<ObjectResult>(result);
            Assert.Equal(500, obj.StatusCode);
        }

        // ----- GetVerificationById -----
        [Fact]
        public async Task GetVerificationById_Found_ReturnsOk()
        {
            var id = Guid.NewGuid();
            var dto = new TutorVerificationDto { VerificationId = id };
            _mockVerificationService.Setup(s => s.GetVerificationByIdAsync(id)).ReturnsAsync(dto);

            var result = await _controller.GetVerificationById(id);

            var ok = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(dto, ok.Value);
        }

        [Fact]
        public async Task GetVerificationById_NotFound_ReturnsNotFound()
        {
            var id = Guid.NewGuid();
            _mockVerificationService.Setup(s => s.GetVerificationByIdAsync(id)).ReturnsAsync((TutorVerificationDto?)null);

            var result = await _controller.GetVerificationById(id);

            Assert.IsType<NotFoundObjectResult>(result);
        }

        [Fact]
        public async Task GetVerificationById_ServerError_Returns500()
        {
            var id = Guid.NewGuid();
            _mockVerificationService.Setup(s => s.GetVerificationByIdAsync(id)).ThrowsAsync(new Exception("boom"));

            var result = await _controller.GetVerificationById(id);

            var obj = Assert.IsType<ObjectResult>(result);
            Assert.Equal(500, obj.StatusCode);
        }

        // ----- GetVerificationByUserId -----
        [Fact]
        public async Task GetVerificationByUserId_TutorNotOwner_ReturnsForbid()
        {
            var userId = Guid.NewGuid();
            SetupUserClaims(userId, "tutor");

            var result = await _controller.GetVerificationByUserId(Guid.NewGuid());

            Assert.IsType<ForbidResult>(result);
        }

        [Fact]
        public async Task GetVerificationByUserId_Found_ReturnsOk()
        {
            var userId = Guid.NewGuid();
            SetupUserClaims(Guid.NewGuid(), "admin");
            var dto = new TutorVerificationDto { VerificationId = Guid.NewGuid(), UserId = userId };
            _mockVerificationService.Setup(s => s.GetVerificationByUserIdAsync(userId)).ReturnsAsync(dto);

            var result = await _controller.GetVerificationByUserId(userId);

            var ok = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(dto, ok.Value);
        }

        [Fact]
        public async Task GetVerificationByUserId_NotFound_ReturnsNotFound()
        {
            var userId = Guid.NewGuid();
            SetupUserClaims(Guid.NewGuid(), "admin");
            _mockVerificationService.Setup(s => s.GetVerificationByUserIdAsync(userId)).ReturnsAsync((TutorVerificationDto?)null);

            var result = await _controller.GetVerificationByUserId(userId);

            Assert.IsType<NotFoundObjectResult>(result);
        }

        [Fact]
        public async Task GetVerificationByUserId_ServerError_Returns500()
        {
            var userId = Guid.NewGuid();
            SetupUserClaims(Guid.NewGuid(), "admin");
            _mockVerificationService.Setup(s => s.GetVerificationByUserIdAsync(userId)).ThrowsAsync(new Exception("boom"));

            var result = await _controller.GetVerificationByUserId(userId);

            var obj = Assert.IsType<ObjectResult>(result);
            Assert.Equal(500, obj.StatusCode);
        }

        // ----- GetAllVerifications -----
        [Fact]
        public async Task GetAllVerifications_ReturnsOk()
        {
            SetupUserClaims(Guid.NewGuid(), "admin");
            var list = new List<TutorVerificationDto> { new TutorVerificationDto { VerificationId = Guid.NewGuid() } };
            _mockVerificationService.Setup(s => s.GetAllVerificationsAsync()).ReturnsAsync(list);

            var result = await _controller.GetAllVerifications();

            var ok = Assert.IsType<OkObjectResult>(result);
            Assert.NotNull(ok.Value);
        }

        [Fact]
        public async Task GetAllVerifications_ServerError_Returns500()
        {
            SetupUserClaims(Guid.NewGuid(), "admin");
            _mockVerificationService.Setup(s => s.GetAllVerificationsAsync()).ThrowsAsync(new Exception("boom"));

            var result = await _controller.GetAllVerifications();

            var obj = Assert.IsType<ObjectResult>(result);
            Assert.Equal(500, obj.StatusCode);
        }

        // ----- SoftDeleteVerification -----
        [Fact]
        public async Task SoftDeleteVerification_Success_ReturnsOk()
        {
            var id = Guid.NewGuid();
            _mockVerificationService.Setup(s => s.GetVerificationByIdAsync(id)).ReturnsAsync(new TutorVerificationDto { VerificationId = id });
            _mockVerificationService.Setup(s => s.SoftDeleteVerificationAsync(id)).Returns(Task.CompletedTask);

            var result = await _controller.SoftDeleteVerification(id);

            var ok = Assert.IsType<OkObjectResult>(result);
            Assert.NotNull(ok.Value);
        }

        [Fact]
        public async Task SoftDeleteVerification_NotFound_ReturnsNotFound()
        {
            var id = Guid.NewGuid();
            _mockVerificationService.Setup(s => s.GetVerificationByIdAsync(id)).ReturnsAsync((TutorVerificationDto?)null);

            var result = await _controller.SoftDeleteVerification(id);

            Assert.IsType<NotFoundObjectResult>(result);
        }

        [Fact]
        public async Task SoftDeleteVerification_ServerError_Returns500()
        {
            var id = Guid.NewGuid();
            _mockVerificationService.Setup(s => s.GetVerificationByIdAsync(id)).ReturnsAsync(new TutorVerificationDto { VerificationId = id });
            _mockVerificationService.Setup(s => s.SoftDeleteVerificationAsync(id)).ThrowsAsync(new Exception("boom"));

            var result = await _controller.SoftDeleteVerification(id);

            var obj = Assert.IsType<ObjectResult>(result);
            Assert.Equal(500, obj.StatusCode);
        }

        // ----- Status lists -----
        [Fact]
        public async Task GetPendingVerifications_ReturnsOk()
        {
            SetupUserClaims(Guid.NewGuid(), "admin");
            var list = new List<TutorVerificationDto> { new TutorVerificationDto { VerificationId = Guid.NewGuid() } };
            _mockVerificationService.Setup(s => s.GetPendingVerificationsAsync()).ReturnsAsync(list);

            var result = await _controller.GetPendingVerifications();

            var ok = Assert.IsType<OkObjectResult>(result);
            Assert.NotNull(ok.Value);
        }

        [Fact]
        public async Task GetApprovedVerifications_ReturnsOk()
        {
            SetupUserClaims(Guid.NewGuid(), "admin");
            var list = new List<TutorVerificationDto> { new TutorVerificationDto { VerificationId = Guid.NewGuid() } };
            _mockVerificationService.Setup(s => s.GetApprovedVerificationsAsync()).ReturnsAsync(list);

            var result = await _controller.GetApprovedVerifications();

            var ok = Assert.IsType<OkObjectResult>(result);
            Assert.NotNull(ok.Value);
        }

        [Fact]
        public async Task GetRejectedVerifications_ReturnsOk()
        {
            SetupUserClaims(Guid.NewGuid(), "admin");
            var list = new List<TutorVerificationDto> { new TutorVerificationDto { VerificationId = Guid.NewGuid() } };
            _mockVerificationService.Setup(s => s.GetRejectedVerificationsAsync()).ReturnsAsync(list);

            var result = await _controller.GetRejectedVerifications();

            var ok = Assert.IsType<OkObjectResult>(result);
            Assert.NotNull(ok.Value);
        }

        // ----- Approve/Reject -----
        [Fact]
        public async Task ApproveVerification_Success_ReturnsOk()
        {
            var id = Guid.NewGuid();
            _mockVerificationService.Setup(s => s.GetVerificationByIdAsync(id)).ReturnsAsync(new TutorVerificationDto { VerificationId = id });
            _mockVerificationService.Setup(s => s.ApproveVerificationAsync(id)).Returns(Task.CompletedTask);

            var result = await _controller.ApproveVerification(id);

            var ok = Assert.IsType<OkObjectResult>(result);
            Assert.NotNull(ok.Value);
        }

        [Fact]
        public async Task ApproveVerification_NotFound_ReturnsNotFound()
        {
            var id = Guid.NewGuid();
            _mockVerificationService.Setup(s => s.GetVerificationByIdAsync(id)).ReturnsAsync((TutorVerificationDto?)null);

            var result = await _controller.ApproveVerification(id);

            Assert.IsType<NotFoundObjectResult>(result);
        }

        [Fact]
        public async Task ApproveVerification_ServerError_Returns500()
        {
            var id = Guid.NewGuid();
            _mockVerificationService.Setup(s => s.GetVerificationByIdAsync(id)).ReturnsAsync(new TutorVerificationDto { VerificationId = id });
            _mockVerificationService.Setup(s => s.ApproveVerificationAsync(id)).ThrowsAsync(new Exception("boom"));

            var result = await _controller.ApproveVerification(id);

            var obj = Assert.IsType<ObjectResult>(result);
            Assert.Equal(500, obj.StatusCode);
        }

        [Fact]
        public async Task RejectVerification_Success_ReturnsOk()
        {
            var id = Guid.NewGuid();
            _mockVerificationService.Setup(s => s.GetVerificationByIdAsync(id)).ReturnsAsync(new TutorVerificationDto { VerificationId = id });
            _mockVerificationService.Setup(s => s.RejectVerificationAsync(id)).Returns(Task.CompletedTask);

            var result = await _controller.RejectVerification(id);

            var ok = Assert.IsType<OkObjectResult>(result);
            Assert.NotNull(ok.Value);
        }

        [Fact]
        public async Task RejectVerification_NotFound_ReturnsNotFound()
        {
            var id = Guid.NewGuid();
            _mockVerificationService.Setup(s => s.GetVerificationByIdAsync(id)).ReturnsAsync((TutorVerificationDto?)null);

            var result = await _controller.RejectVerification(id);

            Assert.IsType<NotFoundObjectResult>(result);
        }

        [Fact]
        public async Task RejectVerification_ServerError_Returns500()
        {
            var id = Guid.NewGuid();
            _mockVerificationService.Setup(s => s.GetVerificationByIdAsync(id)).ReturnsAsync(new TutorVerificationDto { VerificationId = id });
            _mockVerificationService.Setup(s => s.RejectVerificationAsync(id)).ThrowsAsync(new Exception("boom"));

            var result = await _controller.RejectVerification(id);

            var obj = Assert.IsType<ObjectResult>(result);
            Assert.Equal(500, obj.StatusCode);
        }

        // ----- Deleted records operations -----
        [Fact]
        public async Task GetDeletedVerifications_ReturnsOk()
        {
            SetupUserClaims(Guid.NewGuid(), "admin");
            var list = new List<TutorVerificationDto> { new TutorVerificationDto { VerificationId = Guid.NewGuid() } };
            _mockVerificationService.Setup(s => s.GetDeletedVerificationsAsync()).ReturnsAsync(list);

            var result = await _controller.GetDeletedVerifications();

            var ok = Assert.IsType<OkObjectResult>(result);
            Assert.NotNull(ok.Value);
        }

        [Fact]
        public async Task GetDeletedVerificationById_Found_ReturnsOk()
        {
            var id = Guid.NewGuid();
            var dto = new TutorVerificationDto { VerificationId = id };
            _mockVerificationService.Setup(s => s.GetDeletedVerificationByIdAsync(id)).ReturnsAsync(dto);

            var result = await _controller.GetDeletedVerificationById(id);

            var ok = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(dto, ok.Value);
        }

        [Fact]
        public async Task GetDeletedVerificationById_NotFound_ReturnsNotFound()
        {
            var id = Guid.NewGuid();
            _mockVerificationService.Setup(s => s.GetDeletedVerificationByIdAsync(id)).ReturnsAsync((TutorVerificationDto?)null);

            var result = await _controller.GetDeletedVerificationById(id);

            Assert.IsType<NotFoundObjectResult>(result);
        }

        [Fact]
        public async Task RestoreVerification_Success_ReturnsOk()
        {
            var id = Guid.NewGuid();
            _mockVerificationService.Setup(s => s.GetDeletedVerificationByIdAsync(id)).ReturnsAsync(new TutorVerificationDto { VerificationId = id });
            _mockVerificationService.Setup(s => s.RestoreVerificationAsync(id)).Returns(Task.CompletedTask);

            var result = await _controller.RestoreVerification(id);

            var ok = Assert.IsType<OkObjectResult>(result);
            Assert.NotNull(ok.Value);
        }

        [Fact]
        public async Task RestoreVerification_NotFound_ReturnsNotFound()
        {
            var id = Guid.NewGuid();
            _mockVerificationService.Setup(s => s.GetDeletedVerificationByIdAsync(id)).ReturnsAsync((TutorVerificationDto?)null);

            var result = await _controller.RestoreVerification(id);

            Assert.IsType<NotFoundObjectResult>(result);
        }

        [Fact]
        public async Task RestoreVerification_ServerError_Returns500()
        {
            var id = Guid.NewGuid();
            _mockVerificationService.Setup(s => s.GetDeletedVerificationByIdAsync(id)).ReturnsAsync(new TutorVerificationDto { VerificationId = id });
            _mockVerificationService.Setup(s => s.RestoreVerificationAsync(id)).ThrowsAsync(new Exception("boom"));

            var result = await _controller.RestoreVerification(id);

            var obj = Assert.IsType<ObjectResult>(result);
            Assert.Equal(500, obj.StatusCode);
        }

        [Fact]
        public async Task PermanentlyDeleteVerification_Success_ReturnsOk()
        {
            var id = Guid.NewGuid();
            _mockVerificationService.Setup(s => s.PermanentlyDeleteVerificationAsync(id)).Returns(Task.CompletedTask);

            var result = await _controller.PermanentlyDeleteVerification(id);

            var ok = Assert.IsType<OkObjectResult>(result);
            Assert.NotNull(ok.Value);
        }

        [Fact]
        public async Task PermanentlyDeleteVerification_NotFound_ReturnsNotFound()
        {
            var id = Guid.NewGuid();
            _mockVerificationService.Setup(s => s.PermanentlyDeleteVerificationAsync(id)).ThrowsAsync(new KeyNotFoundException("not"));

            var result = await _controller.PermanentlyDeleteVerification(id);

            Assert.IsType<NotFoundObjectResult>(result);
        }

        [Fact]
        public async Task PermanentlyDeleteVerification_ServerError_Returns500()
        {
            var id = Guid.NewGuid();
            _mockVerificationService.Setup(s => s.PermanentlyDeleteVerificationAsync(id)).ThrowsAsync(new Exception("boom"));

            var result = await _controller.PermanentlyDeleteVerification(id);

            var obj = Assert.IsType<ObjectResult>(result);
            Assert.Equal(500, obj.StatusCode);
        }

        [Fact]
        public void Constructor_NullVerificationService_ThrowsArgumentNullException()
        {
            // Assert
            Assert.Throws<ArgumentNullException>(() =>
                new TutorVerificationController(null!));
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
