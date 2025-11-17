using MathBridgeSystem.Api.Controllers;
using MathBridgeSystem.Application.DTOs;
using MathBridgeSystem.Application.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using System.Security.Claims;
using Xunit;
using Assert = Xunit.Assert;

namespace MathBridgeSystem.Test.Controllers
{
    public class ContractControllerTests
    {
        private readonly Mock<IContractService> _mockContractService;
        private readonly ContractController _controller;
        private readonly Guid _userId = Guid.NewGuid();

        public ContractControllerTests()
        {
            _mockContractService = new Mock<IContractService>();
            _controller = new ContractController(_mockContractService.Object);

            var user = new ClaimsPrincipal(new ClaimsIdentity(new Claim[]
            {
                new Claim("sub", _userId.ToString()),
                new Claim(ClaimTypes.NameIdentifier, _userId.ToString()),
                new Claim(ClaimTypes.Role, "parent")
            }, "mock"));

            _controller.ControllerContext = new ControllerContext()
            {
                HttpContext = new DefaultHttpContext() { User = user }
            };
        }

        [Fact]
        public async Task CreateContract_ValidRequest_ReturnsOk()
        {
            // Arrange
            var request = new CreateContractRequest();
            var contractId = Guid.NewGuid();
            _mockContractService.Setup(s => s.CreateContractAsync(request))
                .ReturnsAsync(contractId);

            // Act
            var result = await _controller.CreateContract(request);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.NotNull(okResult.Value);
            _mockContractService.Verify(s => s.CreateContractAsync(request), Times.Once);
        }

        [Fact]
        public async Task CreateContract_NullRequest_ReturnsBadRequest()
        {
            // Act
            var result = await _controller.CreateContract(null!);

            // Assert
            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task CreateContract_Exception_ReturnsInternalServerError()
        {
            // Arrange
            var request = new CreateContractRequest();
            _mockContractService.Setup(s => s.CreateContractAsync(request))
                .ThrowsAsync(new Exception("Service error"));

            // Act
            var result = await _controller.CreateContract(request);

            // Assert
            var objectResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(500, objectResult.StatusCode);
        }



        [Fact]
        public async Task GetContractsByParent_ValidId_ReturnsOk()
        {
            // Arrange
            var parentId = _userId;
            var contracts = new List<ContractDto>
            {
                new ContractDto { ContractId = Guid.NewGuid() }
            };
            _mockContractService.Setup(s => s.GetContractsByParentAsync(parentId))
                .ReturnsAsync(contracts);

            // Act
            var result = await _controller.GetContractsByParent(parentId);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.NotNull(okResult.Value);
            _mockContractService.Verify(s => s.GetContractsByParentAsync(parentId), Times.Once);
        }

        [Fact]
        public async Task GetContractsByParent_DifferentUserId_ReturnsForbid()
        {
            // Arrange
            var differentParentId = Guid.NewGuid();

            // Act
            var result = await _controller.GetContractsByParent(differentParentId);

            // Assert
            Assert.IsType<ForbidResult>(result);
        }

        [Fact]
        public async Task GetContractsByParent_Exception_ReturnsInternalServerError()
        {
            // Arrange
            var parentId = _userId;
            _mockContractService.Setup(s => s.GetContractsByParentAsync(parentId))
                .ThrowsAsync(new Exception("Database error"));

            // Act
            var result = await _controller.GetContractsByParent(parentId);

            // Assert
            var objectResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(500, objectResult.StatusCode);
        }



        [Fact]
        public void Constructor_NullContractService_ThrowsArgumentNullException()
        {
            // Assert
            Assert.Throws<ArgumentNullException>(() =>
                new ContractController(null!));
        }
    }
}
