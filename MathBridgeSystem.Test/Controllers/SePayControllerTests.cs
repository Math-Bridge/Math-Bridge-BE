using FluentAssertions;
using MathBridgeSystem.Api.Controllers;
using MathBridgeSystem.Application.DTOs.SePay;
using MathBridgeSystem.Application.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using System.Security.Claims;
using Xunit;
using Assert = Xunit.Assert;

namespace MathBridgeSystem.Test.Controllers
{
    public class SePayControllerTests
    {
        private readonly Mock<ISePayService> _mockSePayService;
        private readonly Mock<ILogger<SePayController>> _mockLogger;
        private readonly SePayController _controller;

        public SePayControllerTests()
        {
            _mockSePayService = new Mock<ISePayService>();
            _mockLogger = new Mock<ILogger<SePayController>>();
            _controller = new SePayController(_mockSePayService.Object, _mockLogger.Object);
        }

        [Fact]
        public async Task CreatePayment_ValidRequest_ReturnsOk()
        {
            // Arrange
            var userId = Guid.NewGuid();
            SetupUserClaims(userId);
            var request = new SePayPaymentRequestDto
            {
                Amount = 100000,
                Description = "Test payment"
            };
            var response = new SePayPaymentResponseDto
            {
                Success = true,
                WalletTransactionId = Guid.NewGuid(),
                Message = "Payment created"
            };
            _mockSePayService.Setup(s => s.CreatePaymentRequestAsync(It.IsAny<SePayPaymentRequestDto>()))
                .ReturnsAsync(response);

            // Act
            var result = await _controller.CreatePayment(request);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var returnValue = Assert.IsType<SePayPaymentResponseDto>(okResult.Value);
            Assert.True(returnValue.Success);
            _mockSePayService.Verify(s => s.CreatePaymentRequestAsync(It.IsAny<SePayPaymentRequestDto>()), Times.Once);
        }

        [Fact]
        public async Task CreatePayment_InvalidUserId_ReturnsError()
        {
            // Arrange
            var request = new SePayPaymentRequestDto { Amount = 100000 };

            // Act
            var result = await _controller.CreatePayment(request);

            // Assert
            var objectResult = Assert.IsType<ObjectResult>(result.Result);
            Assert.Equal(500, objectResult.StatusCode);
        }

        [Fact]
        public async Task CreatePayment_AmountZero_ReturnsBadRequest()
        {
            // Arrange
            var userId = Guid.NewGuid();
            SetupUserClaims(userId);
            var request = new SePayPaymentRequestDto { Amount = 0 };

            // Act
            var result = await _controller.CreatePayment(request);

            // Assert
            Assert.IsType<BadRequestObjectResult>(result.Result);
        }

        [Fact]
        public async Task CreatePayment_AmountExceedsLimit_ReturnsBadRequest()
        {
            // Arrange
            var userId = Guid.NewGuid();
            SetupUserClaims(userId);
            var request = new SePayPaymentRequestDto { Amount = 51000000 };

            // Act
            var result = await _controller.CreatePayment(request);

            // Assert
            Assert.IsType<BadRequestObjectResult>(result.Result);
        }

        [Fact]
        public async Task CreatePayment_ServiceFails_ReturnsBadRequest()
        {
            // Arrange
            var userId = Guid.NewGuid();
            SetupUserClaims(userId);
            var request = new SePayPaymentRequestDto { Amount = 100000 };
            var response = new SePayPaymentResponseDto
            {
                Success = false,
                Message = "Payment failed"
            };
            _mockSePayService.Setup(s => s.CreatePaymentRequestAsync(It.IsAny<SePayPaymentRequestDto>()))
                .ReturnsAsync(response);

            // Act
            var result = await _controller.CreatePayment(request);

            // Assert
            Assert.IsType<BadRequestObjectResult>(result.Result);
        }

        [Fact]
        public async Task CreatePayment_Exception_ReturnsInternalServerError()
        {
            // Arrange
            var userId = Guid.NewGuid();
            SetupUserClaims(userId);
            var request = new SePayPaymentRequestDto { Amount = 100000 };
            _mockSePayService.Setup(s => s.CreatePaymentRequestAsync(It.IsAny<SePayPaymentRequestDto>()))
                .ThrowsAsync(new Exception("Database error"));

            // Act
            var result = await _controller.CreatePayment(request);

            // Assert
            var statusCodeResult = Assert.IsType<ObjectResult>(result.Result);
            Assert.Equal(500, statusCodeResult.StatusCode);
        }

        private void SetupUserClaims(Guid userId)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, userId.ToString())
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
