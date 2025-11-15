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

namespace MathBridgeSystem.Test.Controllers.Advanced
{
    public class RescheduleControllerEdgeTests
    {
        private RescheduleController CreateController(Mock<IRescheduleService> svc, Guid userId, string role)
        {
            var controller = new RescheduleController(svc.Object);
            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity(new[]{ new Claim(ClaimTypes.NameIdentifier, userId.ToString()), new Claim(ClaimTypes.Role, role)}, "TestAuth"))
                }
            };
            return controller;
        }

        [Fact]
        public async Task GetById_UnauthorizedParent_Forbid()
        {
            var svc = new Mock<IRescheduleService>();
            var requestId = Guid.NewGuid();
            svc.Setup(s => s.GetByIdAsync(requestId, It.IsAny<Guid>(), "parent"))
                .ThrowsAsync(new UnauthorizedAccessException("You can only view your own reschedule requests."));
            var controller = CreateController(svc, Guid.NewGuid(), "parent");
            var result = await controller.GetById(requestId);
            result.Should().BeOfType<ForbidResult>();
        }

        [Fact]
        public async Task Create_InvalidTime_ReturnsBadRequest()
        {
            var svc = new Mock<IRescheduleService>();
            svc.Setup(s => s.CreateRequestAsync(It.IsAny<Guid>(), It.IsAny<CreateRescheduleRequestDto>()))
                .ThrowsAsync(new ArgumentException("Start time must be 16:00"));
            var controller = CreateController(svc, Guid.NewGuid(), "parent");
            var dto = new CreateRescheduleRequestDto{ BookingId = Guid.NewGuid(), RequestedDate = DateOnly.FromDateTime(DateTime.Today.AddDays(2)), StartTime = new TimeOnly(15,0), EndTime = new TimeOnly(16,30)};
            var result = await controller.Create(dto);
            result.Should().BeOfType<BadRequestObjectResult>();
        }
    }
}
