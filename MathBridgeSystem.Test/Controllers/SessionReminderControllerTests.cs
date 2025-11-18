using MathBridgeSystem.Api.Controllers;
using MathBridgeSystem.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;
using Assert = Xunit.Assert;

namespace MathBridgeSystem.Tests.Controllers
{
    public class SessionReminderControllerTests
    {
        private readonly Mock<ISessionReminderService> _mockSessionReminderService;
        private readonly SessionReminderController _controller;

        public SessionReminderControllerTests()
        {
            _mockSessionReminderService = new Mock<ISessionReminderService>();
            _controller = new SessionReminderController(_mockSessionReminderService.Object);
        }

        [Fact]
        public async Task Trigger24HourReminders_Success_ReturnsOk()
        {
            // Arrange
            _mockSessionReminderService.Setup(s => s.CheckAndSendRemindersAsync())
                .Returns(Task.CompletedTask);

            // Act
            var result = await _controller.Trigger24HourReminders();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.NotNull(okResult.Value);
            _mockSessionReminderService.Verify(s => s.CheckAndSendRemindersAsync(), Times.Once);
        }

        [Fact]
        public async Task Trigger1HourReminders_Success_ReturnsOk()
        {
            // Arrange
            _mockSessionReminderService.Setup(s => s.CheckAndSendRemindersAsync())
                .Returns(Task.CompletedTask);

            // Act
            var result = await _controller.Trigger1HourReminders();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.NotNull(okResult.Value);
            _mockSessionReminderService.Verify(s => s.CheckAndSendRemindersAsync(), Times.Once);
        }

        [Fact]
        public async Task CheckAndSendReminders_Success_ReturnsOk()
        {
            // Arrange
            _mockSessionReminderService.Setup(s => s.CheckAndSendRemindersAsync())
                .Returns(Task.CompletedTask);

            // Act
            var result = await _controller.CheckAndSendReminders();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.NotNull(okResult.Value);
            _mockSessionReminderService.Verify(s => s.CheckAndSendRemindersAsync(), Times.Once);
        }

        [Fact]
        public async Task CheckAndSendReminders_Exception_ReturnsInternalServerError()
        {
            // Arrange
            _mockSessionReminderService.Setup(s => s.CheckAndSendRemindersAsync())
                .ThrowsAsync(new Exception("Service error"));

            // Act
            var result = await _controller.CheckAndSendReminders();

            // Assert
            var statusCodeResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(500, statusCodeResult.StatusCode);
        }

        [Fact]
        public void Constructor_NullSessionReminderService_ThrowsArgumentNullException()
        {
            // Assert
            Assert.Throws<ArgumentNullException>(() =>
                new SessionReminderController(null!));
        }
    }
}
