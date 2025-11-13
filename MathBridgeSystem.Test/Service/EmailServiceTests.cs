using FluentAssertions;
using MathBridgeSystem.Application.Interfaces;
using MathBridgeSystem.Application.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace MathBridgeSystem.Tests.Controllers
{
    public class EmailServiceTests
    {
        private readonly Mock<IConfiguration> _configurationMock;
        private readonly Mock<ILogger<EmailService>> _loggerMock;

        public EmailServiceTests()
        {
            _configurationMock = new Mock<IConfiguration>();
            _loggerMock = new Mock<ILogger<EmailService>>();
        }

        #region Constructor Tests

        [Fact]
        public void Constructor_NullConfiguration_ThrowsArgumentNullException()
        {
            Action act = () => new EmailService(null!, _loggerMock.Object);
            act.Should().Throw<ArgumentNullException>();
        }

        [Fact]
        public void Constructor_NullLogger_ThrowsArgumentNullException()
        {
            Action act = () => new EmailService(_configurationMock.Object, null!);
            act.Should().Throw<ArgumentNullException>();
        }

        [Fact]
        public void Constructor_MissingFromEmail_ThrowsArgumentNullException()
        {
            // Arrange
            _configurationMock.Setup(c => c["Gmail:FromEmail"]).Returns((string?)null);

            // Act
            Action act = () => new EmailService(_configurationMock.Object, _loggerMock.Object);

            // Assert
            act.Should().Throw<ArgumentNullException>();
        }

        #endregion

        #region SendVerificationLinkAsync Tests

        [Fact]
        public async Task SendVerificationLinkAsync_NullEmail_ThrowsArgumentException()
        {
            // Arrange
            SetupValidConfiguration();
            var service = CreateEmailService();

            // Act
            Func<Task> act = () => service.SendVerificationLinkAsync(null!, "https://test.com");

            // Assert
            await act.Should().ThrowAsync<ArgumentException>();
        }

        [Fact]
        public async Task SendVerificationLinkAsync_EmptyEmail_ThrowsArgumentException()
        {
            // Arrange
            SetupValidConfiguration();
            var service = CreateEmailService();

            // Act
            Func<Task> act = () => service.SendVerificationLinkAsync("", "https://test.com");

            // Assert
            await act.Should().ThrowAsync<ArgumentException>();
        }

        [Fact]
        public async Task SendVerificationLinkAsync_NullLink_ThrowsArgumentException()
        {
            // Arrange
            SetupValidConfiguration();
            var service = CreateEmailService();

            // Act
            Func<Task> act = () => service.SendVerificationLinkAsync("test@example.com", null!);

            // Assert
            await act.Should().ThrowAsync<ArgumentException>();
        }

        #endregion

        #region SendResetPasswordLinkAsync Tests

        [Fact]
        public async Task SendResetPasswordLinkAsync_NullEmail_ThrowsArgumentException()
        {
            // Arrange
            SetupValidConfiguration();
            var service = CreateEmailService();

            // Act
            Func<Task> act = () => service.SendResetPasswordLinkAsync(null!, "https://test.com");

            // Assert
            await act.Should().ThrowAsync<ArgumentException>();
        }

        [Fact]
        public async Task SendResetPasswordLinkAsync_EmptyEmail_ThrowsArgumentException()
        {
            // Arrange
            SetupValidConfiguration();
            var service = CreateEmailService();

            // Act
            Func<Task> act = () => service.SendResetPasswordLinkAsync("", "https://test.com");

            // Assert
            await act.Should().ThrowAsync<ArgumentException>();
        }

        #endregion

        #region SendSessionReminderAsync Tests

        [Fact]
        public async Task SendSessionReminderAsync_NullEmail_ThrowsArgumentException()
        {
            // Arrange
            SetupValidConfiguration();
            var service = CreateEmailService();

            // Act
            Func<Task> act = () => service.SendSessionReminderAsync(
                null!, "Student", "Tutor", "2025-11-13", "https://link.com");

            // Assert
            await act.Should().ThrowAsync<ArgumentException>();
        }

        [Fact]
        public async Task SendSessionReminderAsync_EmptyStudentName_ThrowsArgumentException()
        {
            // Arrange
            SetupValidConfiguration();
            var service = CreateEmailService();

            // Act
            Func<Task> act = () => service.SendSessionReminderAsync(
                "test@example.com", "", "Tutor", "2025-11-13", "https://link.com");

            // Assert
            await act.Should().ThrowAsync<ArgumentException>();
        }

        #endregion

        #region Helper Methods

        private void SetupValidConfiguration()
        {
            _configurationMock.Setup(c => c["Gmail:FromEmail"]).Returns("noreply@mathbridge.com");
            _configurationMock.Setup(c => c["GoogleMeet:OAuthCredentialsPath"]).Returns("./oauth_credentials.json");
            _configurationMock.Setup(c => c["GoogleMeet:WorkspaceUserEmail"]).Returns("admin@mathbridge.com");
        }

        private EmailService CreateEmailService()
        {
            try
            {
                return new EmailService(_configurationMock.Object, _loggerMock.Object);
            }
            catch
            {
                // If initialization fails due to missing credentials file, return mock
                return null!;
            }
        }

        #endregion
    }
}

