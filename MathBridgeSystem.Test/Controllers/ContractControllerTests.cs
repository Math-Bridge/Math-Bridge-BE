using MathBridgeSystem.Api.Controllers;
using MathBridgeSystem.Application.Interfaces;
using MathBridgeSystem.Application.DTOs;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;
using MathBridgeSystem.Application.DTOs.Contract;
using FluentAssertions;
using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;

namespace MathBridgeSystem.Tests.Controllers
{
    public class ContractControllerTests
    {
        [Fact]
        public async Task CreateContract_InvalidModel_ReturnsBadRequest()
        {
            var serviceMock = new Mock<IContractService>();
            var ctrl = new ContractController(serviceMock.Object);
            ctrl.ModelState.AddModelError("x", "y");

            var result = await ctrl.CreateContract(null);
            result.Should().BeOfType<BadRequestObjectResult>();
        }

        [Fact]
        public async Task CreateContract_ServiceThrows_Returns500()
        {
            var serviceMock = new Mock<IContractService>();
            serviceMock.Setup(s => s.CreateContractAsync(It.IsAny<CreateContractRequest>())).ThrowsAsync(new Exception("db error"));
            var ctrl = new ContractController(serviceMock.Object);

            var req = new CreateContractRequest { ParentId = Guid.NewGuid(), ChildId = Guid.NewGuid(), PackageId = Guid.NewGuid(), StartDate = DateOnly.FromDateTime(DateTime.UtcNow), EndDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(7)), Status = "pending" };
            var result = await ctrl.CreateContract(req);
            var obj = result as ObjectResult;
            obj.StatusCode.Should().Be(500);
        }

        [Fact]
        public async Task UpdateStatus_ForbidWhenNoClaims_ThrowsUnauthorized()
        {
            var serviceMock = new Mock<IContractService>();
            var ctrl = new ContractController(serviceMock.Object);
            // No user claims -> GetUserIdFromClaims will throw when UpdateStatus is called because it reads claims
            var ex = await Xunit.Assert.ThrowsAsync<UnauthorizedAccessException>(async () => await ctrl.UpdateStatus(Guid.NewGuid(), new UpdateContractStatusRequest { Status = "active" }));
        }

        [Fact]
        public async Task GetContractsByParent_Forbid_WhenDifferentUser()
        {
            var serviceMock = new Mock<IContractService>();
            var ctrl = new ContractController(serviceMock.Object);
            var ctx = new DefaultHttpContext();
            ctx.User = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString()) }));
            ctrl.ControllerContext = new ControllerContext { HttpContext = ctx };

            var result = await ctrl.GetContractsByParent(Guid.NewGuid());
            result.Should().BeOfType<ForbidResult>();
        }

        [Fact]
        public async Task GetContractsByParent_ReturnsOk_WhenSameUser()
        {
            var serviceMock = new Mock<IContractService>();
            var ctrl = new ContractController(serviceMock.Object);
            var userId = Guid.NewGuid();
            var ctx = new DefaultHttpContext();
            ctx.User = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, userId.ToString()) }));
            ctrl.ControllerContext = new ControllerContext { HttpContext = ctx };

            serviceMock.Setup(s => s.GetContractsByParentAsync(userId)).ReturnsAsync(new List<ContractDto>());

            var result = await ctrl.GetContractsByParent(userId);
            result.Should().BeOfType<OkObjectResult>();
        }

        [Fact]
        public async Task GetContractById_NotFound_Returns404()
        {
            var serviceMock = new Mock<IContractService>();
            serviceMock.Setup(s => s.GetContractByIdAsync(It.IsAny<Guid>())).ThrowsAsync(new KeyNotFoundException());
            var ctrl = new ContractController(serviceMock.Object);

            var result = await ctrl.GetContractById(Guid.NewGuid());
            result.Should().BeOfType<NotFoundObjectResult>();
        }

        [Fact]
        public async Task GetContractsByParentPhone_BadRequest_WhenEmpty()
        {
            var serviceMock = new Mock<IContractService>();
            var ctrl = new ContractController(serviceMock.Object);

            var result = await ctrl.GetContractsByParentPhone("");
            result.Should().BeOfType<BadRequestObjectResult>();
        }

        [Fact]
        public async Task GetAvailableTutors_ReturnsOk_EmptyList()
        {
            var serviceMock = new Mock<IContractService>();
            serviceMock.Setup(s => s.GetAvailableTutorsAsync(It.IsAny<Guid>())).ReturnsAsync(new List<AvailableTutorResponse>());
            var ctrl = new ContractController(serviceMock.Object);

            var result = await ctrl.GetAvailableTutors(Guid.NewGuid());
            result.Should().BeOfType<OkObjectResult>();
        }
    }
}
