using MathBridgeSystem.Api.Controllers;
using MathBridgeSystem.Application.DTOs;
using MathBridgeSystem.Application.Interfaces;
using MathBridgeSystem.Application.DTOs.Contract;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using System.Security.Claims;
using Xunit;
using Assert = Xunit.Assert;

namespace MathBridgeSystem.Tests.Controllers
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

        private void SetUserRole(string role)
        {
            var user = new ClaimsPrincipal(new ClaimsIdentity(new Claim[]
            {
                new Claim("sub", _userId.ToString()),
                new Claim(ClaimTypes.NameIdentifier, _userId.ToString()),
                new Claim(ClaimTypes.Role, role)
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
        public async Task UpdateStatus_Success_ReturnsOk()
        {
            // Arrange
            SetUserRole("staff");
            var contractId = Guid.NewGuid();
            var request = new UpdateContractStatusRequest { Status = "Approved" };
            _mockContractService.Setup(s => s.UpdateContractStatusAsync(contractId, request, _userId))
                .ReturnsAsync(true);

            // Act
            var result = await _controller.UpdateStatus(contractId, request);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.NotNull(okResult.Value);
            _mockContractService.Verify(s => s.UpdateContractStatusAsync(contractId, request, _userId), Times.Once);
        }

        [Fact]
        public async Task UpdateStatus_ServiceThrows_ReturnsBadRequest()
        {
            // Arrange
            SetUserRole("staff");
            var contractId = Guid.NewGuid();
            var request = new UpdateContractStatusRequest { Status = "Rejected" };
            _mockContractService.Setup(s => s.UpdateContractStatusAsync(contractId, request, _userId))
                .ThrowsAsync(new Exception("update error"));

            // Act
            var result = await _controller.UpdateStatus(contractId, request);

            // Assert
            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task AssignTutors_NullRequest_ReturnsBadRequest()
        {
            // Arrange
            SetUserRole("staff");

            // Act
            var result = await _controller.AssignTutors(Guid.NewGuid(), null!);

            // Assert
            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task AssignTutors_Success_ReturnsOk()
        {
            // Arrange
            SetUserRole("staff");
            var contractId = Guid.NewGuid();
            var request = new AssignTutorToContractRequest { MainTutorId = Guid.NewGuid() };
            _mockContractService.Setup(s => s.AssignTutorsAsync(contractId, request, _userId))
                .ReturnsAsync(true);

            // Act
            var result = await _controller.AssignTutors(contractId, request);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.NotNull(okResult.Value);
            _mockContractService.Verify(s => s.AssignTutorsAsync(contractId, request, _userId), Times.Once);
        }

        [Fact]
        public async Task GetAllContracts_ReturnsOk()
        {
            // Arrange
            var contracts = new List<ContractDto> { new ContractDto { ContractId = Guid.NewGuid() } };
            _mockContractService.Setup(s => s.GetAllContractsAsync()).ReturnsAsync(contracts);

            // Act
            var result = await _controller.GetAllContracts();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.NotNull(okResult.Value);
        }

        [Fact]
        public async Task GetContractById_ReturnsOk()
        {
            // Arrange
            var contractId = Guid.NewGuid();
            var dto = new ContractDto { ContractId = contractId };
            _mockContractService.Setup(s => s.GetContractByIdAsync(contractId)).ReturnsAsync(dto);

            // Act
            var result = await _controller.GetContractById(contractId);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.NotNull(okResult.Value);
        }

        [Fact]
        public async Task GetContractById_NotFound_ReturnsNotFound()
        {
            // Arrange
            var contractId = Guid.NewGuid();
            _mockContractService.Setup(s => s.GetContractByIdAsync(contractId)).ThrowsAsync(new KeyNotFoundException());

            // Act
            var result = await _controller.GetContractById(contractId);

            // Assert
            Assert.IsType<NotFoundObjectResult>(result);
        }

        [Fact]
        public async Task GetContractById_Exception_ReturnsInternalServerError()
        {
            // Arrange
            var contractId = Guid.NewGuid();
            _mockContractService.Setup(s => s.GetContractByIdAsync(contractId)).ThrowsAsync(new Exception("db error"));

            // Act
            var result = await _controller.GetContractById(contractId);

            // Assert
            var objectResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(500, objectResult.StatusCode);
        }

        [Fact]
        public async Task GetContractsByParentPhone_EmptyPhone_ReturnsBadRequest()
        {
            // Act
            var result = await _controller.GetContractsByParentPhone("" );

            // Assert
            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task GetContractsByParentPhone_NotFound_ReturnsNotFound()
        {
            // Arrange
            var phone = "0123456789";
            _mockContractService.Setup(s => s.GetContractsByParentPhoneAsync(phone)).ReturnsAsync(new List<ContractDto>());

            // Act
            var result = await _controller.GetContractsByParentPhone(phone);

            // Assert
            Assert.IsType<NotFoundObjectResult>(result);
        }

        [Fact]
        public async Task GetContractsByParentPhone_ArgumentException_ReturnsBadRequest()
        {
            // Arrange
            var phone = "bad";
            _mockContractService.Setup(s => s.GetContractsByParentPhoneAsync(phone)).ThrowsAsync(new ArgumentException("invalid phone"));

            // Act
            var result = await _controller.GetContractsByParentPhone(phone);

            // Assert
            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task GetContractsByParentPhone_Exception_ReturnsInternalServerError()
        {
            // Arrange
            var phone = "012";
            _mockContractService.Setup(s => s.GetContractsByParentPhoneAsync(phone)).ThrowsAsync(new Exception("boom"));

            // Act
            var result = await _controller.GetContractsByParentPhone(phone);

            // Assert
            var objectResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(500, objectResult.StatusCode);
        }

        [Fact]
        public async Task CompleteContract_Success_ReturnsOk()
        {
            // Arrange
            SetUserRole("staff");
            var contractId = Guid.NewGuid();
            _mockContractService.Setup(s => s.CompleteContractAsync(contractId, _userId)).ReturnsAsync(true);

            // Act
            var result = await _controller.CompleteContract(contractId);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.NotNull(okResult.Value);
        }

        [Fact]
        public async Task CompleteContract_NotFound_ReturnsNotFound()
        {
            // Arrange
            SetUserRole("staff");
            var contractId = Guid.NewGuid();
            _mockContractService.Setup(s => s.CompleteContractAsync(contractId, _userId)).ThrowsAsync(new KeyNotFoundException());

            // Act
            var result = await _controller.CompleteContract(contractId);

            // Assert
            Assert.IsType<NotFoundObjectResult>(result);
        }

        [Fact]
        public async Task CompleteContract_InvalidOperation_ReturnsBadRequest()
        {
            // Arrange
            SetUserRole("staff");
            var contractId = Guid.NewGuid();
            _mockContractService.Setup(s => s.CompleteContractAsync(contractId, _userId)).ThrowsAsync(new InvalidOperationException("cannot complete"));

            // Act
            var result = await _controller.CompleteContract(contractId);

            // Assert
            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task CompleteContract_Exception_ReturnsInternalServerError()
        {
            // Arrange
            SetUserRole("staff");
            var contractId = Guid.NewGuid();
            _mockContractService.Setup(s => s.CompleteContractAsync(contractId, _userId)).ThrowsAsync(new Exception("boom"));

            // Act
            var result = await _controller.CompleteContract(contractId);

            // Assert
            var objectResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(500, objectResult.StatusCode);
        }

        [Fact]
        public async Task GetAvailableTutors_EmptyList_ReturnsOkEmptyList()
        {
            // Arrange
            SetUserRole("staff");
            var contractId = Guid.NewGuid();
            _mockContractService.Setup(s => s.GetAvailableTutorsAsync(contractId)).Returns(Task.FromResult(new List<MathBridgeSystem.Application.DTOs.Contract.AvailableTutorResponse>()));

            // Act
            var result = await _controller.GetAvailableTutors(contractId);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.IsType<List<object>>(okResult.Value);
        }

        [Fact]
        public async Task GetAvailableTutors_ReturnsListOk()
        {
            // Arrange
            SetUserRole("staff");
            var contractId = Guid.NewGuid();
            var tutors = new List<MathBridgeSystem.Application.DTOs.Contract.AvailableTutorResponse>
            {
                new MathBridgeSystem.Application.DTOs.Contract.AvailableTutorResponse { UserId = Guid.NewGuid(), FullName = "Tutor A", Email = "a@x.com", PhoneNumber = "0123", AverageRating = 4.5m, FeedbackCount = 2 }
            };
            _mockContractService.Setup(s => s.GetAvailableTutorsAsync(contractId)).Returns(Task.FromResult(tutors));

            // Act
            var result = await _controller.GetAvailableTutors(contractId);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.NotNull(okResult.Value);
        }

        [Fact]
        public async Task GetAvailableTutors_KeyNotFound_ReturnsNotFound()
        {
            // Arrange
            SetUserRole("staff");
            var contractId = Guid.NewGuid();
            _mockContractService.Setup(s => s.GetAvailableTutorsAsync(contractId)).ThrowsAsync(new KeyNotFoundException("not found"));

            // Act
            var result = await _controller.GetAvailableTutors(contractId);

            // Assert
            Assert.IsType<NotFoundObjectResult>(result);
        }

        [Fact]
        public async Task GetAvailableTutors_InvalidOperation_ReturnsBadRequest()
        {
            // Arrange
            SetUserRole("staff");
            var contractId = Guid.NewGuid();
            _mockContractService.Setup(s => s.GetAvailableTutorsAsync(contractId)).ThrowsAsync(new InvalidOperationException("bad"));

            // Act
            var result = await _controller.GetAvailableTutors(contractId);

            // Assert
            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task GetAvailableTutors_Exception_ReturnsInternalServerError()
        {
            // Arrange
            SetUserRole("staff");
            var contractId = Guid.NewGuid();
            _mockContractService.Setup(s => s.GetAvailableTutorsAsync(contractId)).ThrowsAsync(new Exception("boom"));

            // Act
            var result = await _controller.GetAvailableTutors(contractId);

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
