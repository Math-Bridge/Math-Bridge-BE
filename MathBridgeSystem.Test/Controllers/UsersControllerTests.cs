using MathBridgeSystem.Api.Controllers;
using MathBridgeSystem.Application.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using MathBridgeSystem.Application.DTOs;
using System.Collections.Generic;
using Xunit;
using FluentAssertions;
using System.Security.Claims;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MathBridgeSystem.Tests.Controllers
{
    public class UsersControllerTests
    {
        [Fact]
        public async Task GetAllUsers_Unauthorized_WhenRoleMissing()
        {
            var mock = new Mock<IUserService>();
            var ctrl = new UsersController(mock.Object);
            var ctx = new DefaultHttpContext();
            ctx.User = new ClaimsPrincipal(new ClaimsIdentity());
            ctrl.ControllerContext = new ControllerContext { HttpContext = ctx };

            var result = await ctrl.GetAllUsers();
            result.Should().BeOfType<UnauthorizedObjectResult>();
        }

        [Fact]
        public async Task GetAllUsers_ReturnsOk_WhenRolePresent()
        {
            var mock = new Mock<IUserService>();
            mock.Setup(u => u.GetAllUsersAsync(It.IsAny<string>())).ReturnsAsync(new List<UserResponse>());

            var ctrl = new UsersController(mock.Object);
            var ctx = new DefaultHttpContext();
            ctx.User = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.Role, "admin") }));
            ctrl.ControllerContext = new ControllerContext { HttpContext = ctx };

            var result = await ctrl.GetAllUsers();
            result.Should().BeOfType<OkObjectResult>();
        }

        [Fact]
        public async Task GetUserById_Unauthorized_WhenRoleMissing()
        {
            var mock = new Mock<IUserService>();
            var ctrl = new UsersController(mock.Object);
            var ctx = new DefaultHttpContext();
            ctx.User = new ClaimsPrincipal(new ClaimsIdentity());
            ctrl.ControllerContext = new ControllerContext { HttpContext = ctx };

            var result = await ctrl.GetUserById(Guid.NewGuid());
            result.Should().BeOfType<UnauthorizedObjectResult>();
        }

        [Fact]
        public async Task GetUserById_NotFound_WhenServiceThrowsNotFound()
        {
            var mock = new Mock<IUserService>();
            mock.Setup(u => u.GetUserByIdAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<string>())).ThrowsAsync(new Exception("user not found"));

            var ctrl = new UsersController(mock.Object);
            var ctx = new DefaultHttpContext();
            ctx.User = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.Role, "admin"), new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString()) }));
            ctrl.ControllerContext = new ControllerContext { HttpContext = ctx };

            var result = await ctrl.GetUserById(Guid.NewGuid());
            result.Should().BeOfType<NotFoundObjectResult>();
        }

        [Fact]
        public async Task UpdateUser_BadRequest_WhenModelInvalid()
        {
            var mock = new Mock<IUserService>();
            var ctrl = new UsersController(mock.Object);
            ctrl.ModelState.AddModelError("x", "y");

            var result = await ctrl.UpdateUser(Guid.NewGuid(), new UpdateUserRequest());
            result.Should().BeOfType<BadRequestObjectResult>();
        }

        [Fact]
        public async Task GetWallet_Unauthorized_WhenRoleMissing()
        {
            var mock = new Mock<IUserService>();
            var ctrl = new UsersController(mock.Object);
            var ctx = new DefaultHttpContext();
            ctx.User = new ClaimsPrincipal(new ClaimsIdentity());
            ctrl.ControllerContext = new ControllerContext { HttpContext = ctx };

            var result = await ctrl.GetWallet(Guid.NewGuid());
            result.Should().BeOfType<UnauthorizedObjectResult>();
        }
    }
}
