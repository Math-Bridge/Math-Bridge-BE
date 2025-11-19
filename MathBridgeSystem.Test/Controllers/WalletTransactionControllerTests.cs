using FluentAssertions;
using MathBridgeSystem.Api.Controllers;
using MathBridgeSystem.Application.DTOs.WalletTransaction;
using MathBridgeSystem.Application.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using System.Security.Claims;
using Xunit;

namespace MathBridgeSystem.Tests.Controllers
{
    public class WalletTransactionControllerTests
    {
        private readonly Mock<IWalletTransactionService> _serviceMock;
        private readonly WalletTransactionController _controller;
        private readonly Guid _userId = Guid.NewGuid();

        public WalletTransactionControllerTests()
        {
            _serviceMock = new Mock<IWalletTransactionService>();
            _controller = new WalletTransactionController(_serviceMock.Object);
        }

        private void SetupUser(string role)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, _userId.ToString()),
                new Claim(ClaimTypes.Role, role)
            };
            _controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity(claims)) } };
        }

        [Fact]
        public async Task GetTransactionById_ParentNotOwner_ReturnsForbid()
        {
            var txId = Guid.NewGuid();
            var dto = new WalletTransactionDto { TransactionId = txId, ParentId = Guid.NewGuid() };
            _serviceMock.Setup(s => s.GetTransactionByIdAsync(txId)).ReturnsAsync(dto);

            SetupUser("parent");
            var result = await _controller.GetTransactionById(txId);

            result.Should().BeOfType<ForbidResult>();
        }

        [Fact]
        public async Task GetTransactionById_NotFound_ReturnsNotFound()
        {
            var txId = Guid.NewGuid();
            _serviceMock.Setup(s => s.GetTransactionByIdAsync(txId)).ThrowsAsync(new KeyNotFoundException("not"));

            SetupUser("admin");
            var result = await _controller.GetTransactionById(txId);

            result.Should().BeOfType<NotFoundObjectResult>();
        }

        [Fact]
        public async Task GetTransactionsByParentId_ParentMismatch_ReturnsForbid()
        {
            var parentId = Guid.NewGuid();
            SetupUser("parent");

            var result = await _controller.GetTransactionsByParentId(parentId);

            result.Should().BeOfType<ForbidResult>();
        }

        [Fact]
        public async Task GetMyTransactions_ReturnsOk()
        {
            SetupUser("parent");
            _serviceMock.Setup(s => s.GetTransactionsByParentIdAsync(_userId)).ReturnsAsync(new List<WalletTransactionDto>());

            var result = await _controller.GetMyTransactions();

            result.Should().BeOfType<OkObjectResult>();
        }

        [Fact]
        public async Task GetWalletBalance_ParentMismatch_ReturnsForbid()
        {
            var parentId = Guid.NewGuid();
            SetupUser("parent");

            var result = await _controller.GetWalletBalance(parentId);

            result.Should().BeOfType<ForbidResult>();
        }

        [Fact]
        public async Task GetWallet_NotFound_ReturnsNotFound()
        {
            var parentId = Guid.NewGuid();
            _serviceMock.Setup(s => s.GetParentWalletBalanceAsync(parentId)).ThrowsAsync(new KeyNotFoundException("not"));

            SetupUser("admin");
            var result = await _controller.GetWalletBalance(parentId);

            result.Should().BeOfType<NotFoundObjectResult>();
        }

        [Fact]
        public async Task GetMyBalance_ReturnsOk()
        {
            SetupUser("parent");
            _serviceMock.Setup(s => s.GetParentWalletBalanceAsync(_userId)).ReturnsAsync(123m);

            var result = await _controller.GetMyBalance();

            var ok = result.Should().BeOfType<OkObjectResult>().Subject;
            ok.Value.Should().NotBeNull();
        }

        [Fact]
        public async Task CreateTransaction_InvalidModel_ReturnsBadRequest()
        {
            _controller.ModelState.AddModelError("Amount", "Required");

            var result = await _controller.CreateTransaction(new CreateWalletTransactionRequest());

            result.Should().BeOfType<BadRequestObjectResult>();
        }

        [Fact]
        public async Task CreateTransaction_ArgumentException_ReturnsBadRequest()
        {
            var req = new CreateWalletTransactionRequest { ParentId = Guid.NewGuid(), Amount = 10m, TransactionType = "Deposit" };
            _serviceMock.Setup(s => s.CreateTransactionAsync(req)).ThrowsAsync(new ArgumentException("bad"));

            SetupUser("admin");
            var result = await _controller.CreateTransaction(req);

            result.Should().BeOfType<BadRequestObjectResult>();
        }

        [Fact]
        public async Task CreateTransaction_ServerError_Returns500()
        {
            var req = new CreateWalletTransactionRequest { ParentId = Guid.NewGuid(), Amount = 10m, TransactionType = "Deposit" };
            _serviceMock.Setup(s => s.CreateTransactionAsync(req)).ThrowsAsync(new Exception("boom"));

            SetupUser("admin");
            var result = await _controller.CreateTransaction(req);

            var obj = result.Should().BeOfType<ObjectResult>().Subject;
            obj.StatusCode.Should().Be(500);
        }

        [Fact]
        public async Task UpdateTransactionStatus_ModelInvalid_ReturnsBadRequest()
        {
            _controller.ModelState.AddModelError("Status", "Required");
            var result = await _controller.UpdateTransactionStatus(Guid.NewGuid(), new UpdateTransactionStatusRequest());
            result.Should().BeOfType<BadRequestObjectResult>();
        }

        [Fact]
        public async Task UpdateTransactionStatus_NotFound_ReturnsNotFound()
        {
            _serviceMock.Setup(s => s.UpdateTransactionStatusAsync(It.IsAny<Guid>(), It.IsAny<string>())).ThrowsAsync(new KeyNotFoundException("not"));
            SetupUser("admin");

            var result = await _controller.UpdateTransactionStatus(Guid.NewGuid(), new UpdateTransactionStatusRequest { Status = "Completed" });

            result.Should().BeOfType<NotFoundObjectResult>();
        }

        [Fact]
        public async Task UpdateTransactionStatus_Success_ReturnsOk()
        {
            _serviceMock.Setup(s => s.UpdateTransactionStatusAsync(It.IsAny<Guid>(), "Completed")).Returns(Task.CompletedTask);
            SetupUser("admin");

            var result = await _controller.UpdateTransactionStatus(Guid.NewGuid(), new UpdateTransactionStatusRequest { Status = "Completed" });

            result.Should().BeOfType<OkObjectResult>();
        }
    }
}
