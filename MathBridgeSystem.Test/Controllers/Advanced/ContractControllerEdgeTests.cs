using System;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using Xunit;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;
using MathBridgeSystem.Application.Interfaces;
using MathBridgeSystem.Api.Controllers;
using MathBridgeSystem.Application.DTOs;
using System.Collections.Generic;

namespace MathBridgeSystem.Test.Controllers.Advanced
{
    public class ContractControllerEdgeTests
    {
        private ContractController CreateController(Mock<IContractService> svc, Guid userId, string role)
        {
            var controller = new ContractController(svc.Object);
            var claims = new List<Claim>{ new Claim(ClaimTypes.NameIdentifier, userId.ToString()), new Claim(ClaimTypes.Role, role) };
            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth"))
                }
            };
            return controller;
        }

        [Fact]
        public async Task GetContractsByParent_Forbid_WhenDifferentUser()
        {
            var svc = new Mock<IContractService>();
            var parentId = Guid.NewGuid();
            var otherUser = Guid.NewGuid();
            var controller = CreateController(svc, otherUser, "parent");
            var result = await controller.GetContractsByParent(parentId);
            result.Should().BeOfType<ForbidResult>();
        }

        [Fact]
        public async Task UpdateStatus_BadStatus_ReturnsBadRequest()
        {
            var svc = new Mock<IContractService>();
            var controller = CreateController(svc, Guid.NewGuid(), "staff");
            svc.Setup(s => s.UpdateContractStatusAsync(It.IsAny<Guid>(), It.IsAny<UpdateContractStatusRequest>(), It.IsAny<Guid>()))
                .ThrowsAsync(new ArgumentException("Invalid status"));
            var result = await controller.UpdateStatus(Guid.NewGuid(), new UpdateContractStatusRequest{ Status="weird"});
            var bad = result as BadRequestObjectResult;
            bad.Should().NotBeNull();
        }

        [Fact]
        public async Task AssignTutors_NullRequest_ReturnsBadRequest()
        {
            var svc = new Mock<IContractService>();
            var controller = CreateController(svc, Guid.NewGuid(), "staff");
            var result = await controller.AssignTutors(Guid.NewGuid(), null!);
            result.Should().BeOfType<BadRequestObjectResult>();
        }
    }
}
