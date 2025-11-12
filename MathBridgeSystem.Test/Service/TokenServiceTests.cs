using FluentAssertions;
using MathBridgeSystem.Application.Interfaces;
using MathBridgeSystem.Application.Services;
using Microsoft.Extensions.Configuration;
using Moq;
using System;
using System.IdentityModel.Tokens.Jwt; 
using System.Linq;
using System.Security.Claims; 
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace MathBridgeSystem.Tests.Services
{
    public class TokenServiceTests
    {
        private readonly Mock<IConfiguration> _configurationMock;
        private readonly TokenService _tokenService;

        private const string FakeJwtKey = "DayLaMotCaiKeyBiMatRatDaiDeTestHmacSha256";
        private const string FakeIssuer = "TestIssuer";
        private const string FakeAudience = "TestAudience";

        public TokenServiceTests()
        {
            _configurationMock = new Mock<IConfiguration>();

            _configurationMock.SetupGet(c => c["Jwt:Key"]).Returns(FakeJwtKey);
            _configurationMock.SetupGet(c => c["Jwt:Issuer"]).Returns(FakeIssuer);
            _configurationMock.SetupGet(c => c["Jwt:Audience"]).Returns(FakeAudience);

            _tokenService = new TokenService(_configurationMock.Object);
        }

        // Test: Ném lỗi nếu IConfiguration là null
        [Fact]
        public void Constructor_NullConfiguration_ThrowsArgumentNullException()
        {
            // Act
            Action act = () => new TokenService(null);

            // Assert
            act.Should().Throw<ArgumentNullException>().WithParameterName("configuration");
        }

        // Test: Ném lỗi nếu role là null
        [Fact]
        public void GenerateJwtToken_NullRole_ThrowsArgumentException()
        {
            // Arrange
            var userId = Guid.NewGuid();
            string role = null;

            // Act
            Action act = () => _tokenService.GenerateJwtToken(userId, role);

            // Assert
            act.Should().Throw<ArgumentException>()
                .WithParameterName("role")
                .WithMessage("Role cannot be null or empty (Parameter 'role')");
        }

        // Test: Ném lỗi nếu role là rỗng
        [Fact]
        public void GenerateJwtToken_EmptyRole_ThrowsArgumentException()
        {
            // Arrange
            var userId = Guid.NewGuid();
            string role = string.Empty;

            // Act
            Action act = () => _tokenService.GenerateJwtToken(userId, role);

            // Assert
            act.Should().Throw<ArgumentException>()
                .WithParameterName("role")
                .WithMessage("Role cannot be null or empty (Parameter 'role')");
        }

        // Test: Ném lỗi nếu Jwt:Key bị thiếu trong config
        [Fact]
        public void GenerateJwtToken_MissingJwtKey_ThrowsArgumentNullException()
        {
            // Arrange
            _configurationMock.SetupGet(c => c["Jwt:Key"]).Returns((string)null); 
            var serviceWithBadConfig = new TokenService(_configurationMock.Object);
            var userId = Guid.NewGuid();
            var role = "admin";

            // Act
            Action act = () => serviceWithBadConfig.GenerateJwtToken(userId, role);

            // Assert
            act.Should().Throw<ArgumentNullException>();
        }

        // Test: Ném lỗi nếu Jwt:Key quá ngắn (không đủ an toàn)
        [Fact]
        public void GenerateJwtToken_JwtKeyTooShort_ThrowsArgumentOutOfRangeException()
        {
            // Arrange
            _configurationMock.SetupGet(c => c["Jwt:Key"]).Returns("short"); 
            var serviceWithBadConfig = new TokenService(_configurationMock.Object);
            var userId = Guid.NewGuid();
            var role = "admin";

            // Act
            Action act = () => serviceWithBadConfig.GenerateJwtToken(userId, role);

            // Assert
            act.Should().Throw<ArgumentOutOfRangeException>();
        }

        // Test: Tạo token thành công và kiểm tra nội dung
        [Fact]
        public void GenerateJwtToken_ValidInput_ReturnsTokenWithCorrectClaims()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var role = "parent";

            // Act
            var tokenString = _tokenService.GenerateJwtToken(userId, role);

            // Assert
            tokenString.Should().NotBeNullOrEmpty();
            tokenString.Split('.').Length.Should().Be(3); 

            var handler = new JwtSecurityTokenHandler();
            var token = handler.ReadJwtToken(tokenString);

            token.Issuer.Should().Be(FakeIssuer);
            token.Audiences.Should().Contain(FakeAudience);

            var subClaim = token.Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Sub);
            subClaim.Should().NotBeNull();
            subClaim.Value.Should().Be(userId.ToString());

            var roleClaim = token.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Role);
            roleClaim.Should().NotBeNull();
            roleClaim.Value.Should().Be(role);

            var jtiClaim = token.Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Jti);
            jtiClaim.Should().NotBeNull();
            Guid.TryParse(jtiClaim.Value, out _).Should().BeTrue(); 
        }

        // Test: Kiểm tra token hết hạn sau 1 giờ (cho phép sai số 1 phút)
        [Fact]
        public void GenerateJwtToken_ValidInput_TokenExpiresInOneHour()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var role = "admin";
            var expectedExpiration = DateTime.UtcNow.AddHours(1);

            // Act
            var tokenString = _tokenService.GenerateJwtToken(userId, role);

            // Assert
            var handler = new JwtSecurityTokenHandler();
            var token = handler.ReadJwtToken(tokenString);

            token.ValidTo.Should().BeCloseTo(expectedExpiration, TimeSpan.FromMinutes(1));
        }
    }
}