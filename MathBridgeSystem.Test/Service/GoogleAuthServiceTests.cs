using FluentAssertions;
using MathBridgeSystem.Application.Services;
using Xunit;

namespace MathBridgeSystem.Tests.Services
{
    public class GoogleAuthServiceTests
    {
        private readonly GoogleAuthService _service;

        public GoogleAuthServiceTests()
        {
            _service = new GoogleAuthService();
        }

        #region ValidateGoogleTokenAsync Tests

        [Fact]
        public async Task ValidateGoogleTokenAsync_NullToken_ThrowsException()
        {
            // Act
            Func<Task> act = () => _service.ValidateGoogleTokenAsync(null!);

            // Assert
            await act.Should().ThrowAsync<Exception>();
        }

        [Fact]
        public async Task ValidateGoogleTokenAsync_EmptyToken_ThrowsException()
        {
            // Act
            Func<Task> act = () => _service.ValidateGoogleTokenAsync("");

            // Assert
            await act.Should().ThrowAsync<Exception>();
        }

        [Fact]
        public async Task ValidateGoogleTokenAsync_InvalidToken_ThrowsException()
        {
            // Arrange
            var invalidToken = "invalid_token_12345";

            // Act
            Func<Task> act = () => _service.ValidateGoogleTokenAsync(invalidToken);

            // Assert
            await act.Should().ThrowAsync<Exception>()
                ; // message may vary depending on underlying implementation
        }

        [Fact]
        public async Task ValidateGoogleTokenAsync_MalformedToken_ThrowsException()
        {
            // Arrange
            var malformedToken = "malformed.token.here";

            // Act
            Func<Task> act = () => _service.ValidateGoogleTokenAsync(malformedToken);

            // Assert
            await act.Should().ThrowAsync<Exception>();
        }

        #endregion

        #region Edge Cases

        [Fact]
        public async Task ValidateGoogleTokenAsync_WhitespaceToken_ThrowsException()
        {
            // Arrange
            var whitespaceToken = "   ";

            // Act
            Func<Task> act = () => _service.ValidateGoogleTokenAsync(whitespaceToken);

            // Assert
            await act.Should().ThrowAsync<Exception>();
        }

        [Theory]
        [InlineData("")]
        [InlineData(" ")]
        [InlineData(null)]
        public async Task ValidateGoogleTokenAsync_InvalidTokenVariants_ThrowsException(string? token)
        {
            // Act
            Func<Task> act = () => _service.ValidateGoogleTokenAsync(token!);

            // Assert
            await act.Should().ThrowAsync<Exception>();
        }

        #endregion
    }
}

