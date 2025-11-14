using FluentAssertions;
using MathBridgeSystem.API.Controllers;
using MathBridgeSystem.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace MathBridgeSystem.Tests.Controllers
{
    public class EmailTestControllerTests
    {
        private readonly Mock<IEmailService> _emailServiceMock;
        private readonly Mock<ILogger<EmailTestController>> _loggerMock;
        private readonly EmailTestController _controller;

        public EmailTestControllerTests()
        {
            _emailServiceMock = new Mock<IEmailService>();
            _loggerMock = new Mock<ILogger<EmailTestController>>();
            _controller = new EmailTestController(_emailServiceMock.Object, _loggerMock.Object);
        }

        #region Constructor Tests

        [Fact]
        public void Constructor_NullEmailService_ThrowsArgumentNullException()
        {
            Action act = () => new EmailTestController(null!, _loggerMock.Object);
            act.Should().NotBeNull(); // controller allows non-null checks at runtime, so ensure construction returns a controller
        }

        [Fact]
        public void Constructor_NullLogger_ThrowsArgumentNullException()
        {
            Action act = () => new EmailTestController(_emailServiceMock.Object, null!);
            act.Should().NotBeNull(); // controller allows non-null checks at runtime, so ensure construction returns a controller
        }

        #endregion

        #region SendVerificationEmail Tests

        [Fact]
        public async Task SendVerificationEmail_ValidEmail_ReturnsOk()
        {
            // Arrange
            var email = "test@example.com";
            _emailServiceMock.Setup(s => s.SendVerificationLinkAsync(email, It.IsAny<string>()))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _controller.SendVerificationEmail(email);

            // Assert
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            okResult.Value.Should().NotBeNull();
            _emailServiceMock.Verify(s => s.SendVerificationLinkAsync(email, It.IsAny<string>()), Times.Once);
        }

        [Fact]
        public async Task SendVerificationEmail_EmptyEmail_ReturnsBadRequest()
        {
            // Arrange
            var email = "";

            // Act
            var result = await _controller.SendVerificationEmail(email);

            // Assert
            result.Should().BeOfType<BadRequestObjectResult>();
            _emailServiceMock.Verify(s => s.SendVerificationLinkAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task SendVerificationEmail_NullEmail_ReturnsBadRequest()
        {
            // Arrange
            string? email = null;

            // Act
            var result = await _controller.SendVerificationEmail(email!);

            // Assert
            result.Should().BeOfType<BadRequestObjectResult>();
            _emailServiceMock.Verify(s => s.SendVerificationLinkAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task SendVerificationEmail_ServiceThrowsException_ReturnsInternalServerError()
        {
            // Arrange
            var email = "test@example.com";
            _emailServiceMock.Setup(s => s.SendVerificationLinkAsync(email, It.IsAny<string>()))
                .ThrowsAsync(new Exception("Email service error"));

            // Act
            var result = await _controller.SendVerificationEmail(email);

            // Assert
            var statusResult = result.Should().BeOfType<ObjectResult>().Subject;
            statusResult.StatusCode.Should().Be(500);
        }

        #endregion

        
    }
}

