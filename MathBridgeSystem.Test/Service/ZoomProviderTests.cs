using FluentAssertions;
using MathBridgeSystem.Application.DTOs.VideoConference;
using MathBridgeSystem.Application.Interfaces;
using MathBridgeSystem.Infrastructure.Services;
using Microsoft.Extensions.Configuration;
using Moq;
using Moq.Protected;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text;
using Xunit;
using System.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Threading;
using MathBridgeSystem.Application.Services;

namespace MathBridgeSystem.Tests.Services
{
    public class MockHttpMessageHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> _handlerFunc;

        public MockHttpMessageHandler(HttpResponseMessage response)
        {
            _handlerFunc = (req, ct) => Task.FromResult(response);
        }

        public MockHttpMessageHandler(Func<HttpRequestMessage, CancellationToken, HttpResponseMessage> handlerFunc)
        {
            _handlerFunc = (req, ct) => Task.FromResult(handlerFunc(req, ct));
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return _handlerFunc(request, cancellationToken);
        }
    }

    public class ZoomProviderTests
    {
        private readonly Mock<IConfiguration> _configurationMock;
        private readonly Mock<HttpMessageHandler> _httpMessageHandlerMock;
        private readonly HttpClient _httpClient;
        private readonly ZoomProvider _zoomProvider;

        public ZoomProviderTests()
        {
            _configurationMock = new Mock<IConfiguration>();
            _httpMessageHandlerMock = new Mock<HttpMessageHandler>();
            _httpClient = new HttpClient(_httpMessageHandlerMock.Object);

            _configurationMock.SetupGet(c => c["Zoom:UserId"]).Returns("me");
            _configurationMock.SetupGet(c => c["Zoom:ClientId"]).Returns("test_client_id");
            _configurationMock.SetupGet(c => c["Zoom:ClientSecret"]).Returns("test_client_secret");
            _configurationMock.SetupGet(c => c["Zoom:AccountId"]).Returns("test_account_id");

            _zoomProvider = new ZoomProvider(_httpClient, _configurationMock.Object);
        }

        private void MockGetTokenSuccess()
        {
            var tokenResponse = new
            {
                access_token = "fake_token_123",
                expires_in = 3600
            };
            var responseMessage = new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(JsonSerializer.Serialize(tokenResponse), Encoding.UTF8, "application/json")
            };

            _httpMessageHandlerMock.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.Is<HttpRequestMessage>(req => req.RequestUri.ToString().Contains("zoom.us/oauth/token")),
                    ItExpr.IsAny<CancellationToken>()
                )
                .ReturnsAsync(responseMessage);
        }


        // Test: Ném lỗi nếu thiếu Config
        [Fact]
        public async Task GetAccessTokenAsync_MissingCredentials_ThrowsInvalidOperationException()
        {
            // Arrange
            _configurationMock.SetupGet(c => c["Zoom:ClientId"]).Returns((string)null); 
            var provider = new ZoomProvider(_httpClient, _configurationMock.Object); 

            // Act
            var result = await provider.CreateMeetingAsync();

            // Assert
            result.Success.Should().BeFalse();
            result.ErrorMessage.Should().Contain("Zoom credentials not configured");
        }

        // Test: Ném lỗi nếu HTTP request lấy token thất bại
        [Fact]
        public async Task GetAccessTokenAsync_HttpFails_ThrowsInvalidOperationException()
        {
            // Arrange
            var responseMessage = new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.Unauthorized, 
                Content = new StringContent("Invalid credentials")
            };

            _httpMessageHandlerMock.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.Is<HttpRequestMessage>(req => req.RequestUri.ToString().Contains("zoom.us/oauth/token")),
                    ItExpr.IsAny<CancellationToken>()
                )
                .ReturnsAsync(responseMessage);

            // Act
            var result = await _zoomProvider.CreateMeetingAsync();

            // Assert
            result.Success.Should().BeFalse();
            result.ErrorMessage.Should().Contain("Failed to get Zoom access token: Unauthorized");
        }

        // Test: Ném lỗi nếu JSON phản hồi thiếu token
        [Fact]
        public async Task GetAccessTokenAsync_ResponseMissingToken_ThrowsInvalidOperationException()
        {
            // Arrange
            var tokenResponse = new { expires_in = 3600 }; 
            var responseMessage = new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(JsonSerializer.Serialize(tokenResponse), Encoding.UTF8, "application/json")
            };
            _httpMessageHandlerMock.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>()
                ).ReturnsAsync(responseMessage);

            // Act
            var result = await _zoomProvider.CreateMeetingAsync();

            // Assert
            result.Success.Should().BeFalse();
            result.ErrorMessage.Should().Contain("Exception creating Zoom meeting:");
        }

        // Test: Logic cache token (gọi 2 lần, chỉ request HTTP 1 lần)
        [Fact]
        public async Task GetAccessTokenAsync_WithCache_OnlyCallsHttpOnce()
        {
            // Arrange
            MockGetTokenSuccess(); 

            var meetingResponse = new ZoomMeetingResponse { Id = 12345 };
            var responseMessage = new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.Created,
                Content = new StringContent(JsonSerializer.Serialize(meetingResponse), Encoding.UTF8, "application/json")
            };
            _httpMessageHandlerMock.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.Is<HttpRequestMessage>(req => req.RequestUri.ToString().Contains("/meetings")),
                    ItExpr.IsAny<CancellationToken>()
                )
                .ReturnsAsync(responseMessage);

            // Act
            await _zoomProvider.CreateMeetingAsync(); 
            await _zoomProvider.CreateMeetingAsync(); 

            // Assert
            _httpMessageHandlerMock.Protected().Verify(
                "SendAsync",
                Times.Once(),
                ItExpr.Is<HttpRequestMessage>(req => req.RequestUri.ToString().Contains("zoom.us/oauth/token")),
                ItExpr.IsAny<CancellationToken>());

            _httpMessageHandlerMock.Protected().Verify(
                "SendAsync",
                Times.Exactly(2),
                ItExpr.Is<HttpRequestMessage>(req => req.RequestUri.ToString().Contains("/meetings")),
                ItExpr.IsAny<CancellationToken>());
        }

        #region CreateMeetingAsync Tests

        // Test: Tạo meeting thành công
        [Fact]
        public async Task CreateMeetingAsync_Valid_ReturnsSuccessResult()
        {
            // Arrange
            MockGetTokenSuccess(); 

            var meetingResponse = new ZoomMeetingResponse { Id = 987654321 };
            var responseMessage = new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.Created,
                Content = new StringContent(JsonSerializer.Serialize(meetingResponse), Encoding.UTF8, "application/json")
            };

            _httpMessageHandlerMock.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.Is<HttpRequestMessage>(req =>
                        req.RequestUri.ToString().Contains("/users/me/meetings") &&
                        req.Headers.Authorization.Scheme == "Bearer" &&
                        req.Headers.Authorization.Parameter == "fake_token_123"
                    ),
                    ItExpr.IsAny<CancellationToken>()
                )
                .ReturnsAsync(responseMessage);

            // Act
            var result = await _zoomProvider.CreateMeetingAsync();

            // Assert
            result.Success.Should().BeTrue();
            result.MeetingId.Should().Be("987654321");
            result.MeetingUri.Should().Be("https://zoom.us/j/987654321");
            result.MeetingCode.Should().Be("987654321");
        }

        // Test: Tạo meeting thất bại (lỗi HTTP)
        [Fact]
        public async Task CreateMeetingAsync_ApiFails_ReturnsFailureResult()
        {
            // Arrange
            MockGetTokenSuccess(); 

            var responseMessage = new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.BadRequest, 
                Content = new StringContent("Invalid request body")
            };

            _httpMessageHandlerMock.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.Is<HttpRequestMessage>(req => req.RequestUri.ToString().Contains("/meetings")),
                    ItExpr.IsAny<CancellationToken>()
                )
                .ReturnsAsync(responseMessage);

            // Act
            var result = await _zoomProvider.CreateMeetingAsync();

            // Assert
            result.Success.Should().BeFalse();
            result.ErrorMessage.Should().Contain("Zoom API error: BadRequest - Invalid request body");
        }

        // Test: Tạo meeting thất bại (không parse được JSON)
        [Fact]
        public async Task CreateMeetingAsync_InvalidJsonResponse_ReturnsFailureResult()
        {
            // Arrange
            MockGetTokenSuccess();

            var responseMessage = new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.Created,
                Content = new StringContent("null") 
            };

            _httpMessageHandlerMock.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.Is<HttpRequestMessage>(req => req.RequestUri.ToString().Contains("/meetings")),
                    ItExpr.IsAny<CancellationToken>()
                )
                .ReturnsAsync(responseMessage);

            // Act
            var result = await _zoomProvider.CreateMeetingAsync();

            // Assert
            result.Success.Should().BeFalse();
            result.ErrorMessage.Should().Be("Failed to parse Zoom response");
        }

        #endregion
    }

    public class ZoomMeetingResponse
    {
        public long Id { get; set; }
    }
}