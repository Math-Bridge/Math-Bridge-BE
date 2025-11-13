using MathBridgeSystem.Api.Controllers;
using MathBridgeSystem.Application.Interfaces;
using MathBridgeSystem.Infrastructure.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using MathBridgeSystem.Application.DTOs;
using MathBridgeSystem.Application.DTOs.Notification;
using Xunit;
using FluentAssertions;
using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;

namespace MathBridgeSystem.Tests.Controllers
{
    public class NotificationControllerTests
    {
        [Fact]
        public async Task GetUnreadCount_ReturnsCount()
        {
            var userId = Guid.NewGuid();
            var notificationServiceMock = new Mock<INotificationService>();
            var connectionManagerMock = new Mock<NotificationConnectionManager>();
            notificationServiceMock.Setup(s => s.GetUnreadCountAsync(userId)).ReturnsAsync(7);

            var controller = new NotificationController(notificationServiceMock.Object, connectionManagerMock.Object);
            var ctx = new DefaultHttpContext();
            ctx.User = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, userId.ToString()) }));
            controller.ControllerContext = new ControllerContext { HttpContext = ctx };

            var result = await controller.GetUnreadCount();
            result.Result.Should().BeOfType<OkObjectResult>();
            var ok = result.Result as OkObjectResult;
            ok.Value.Should().Be(7);
        }

        [Fact]
        public async Task GetNotification_NotFound_ReturnsNotFound()
        {
            var notificationServiceMock = new Mock<INotificationService>();
            var connectionManagerMock = new Mock<NotificationConnectionManager>();
            notificationServiceMock.Setup(s => s.GetNotificationByIdAsync(It.IsAny<Guid>())).ReturnsAsync((NotificationResponseDto)null);

            var controller = new NotificationController(notificationServiceMock.Object, connectionManagerMock.Object);
            var ctx = new DefaultHttpContext();
            ctx.User = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString()) }));
            controller.ControllerContext = new ControllerContext { HttpContext = ctx };

            var result = await controller.GetNotification(Guid.NewGuid());
            result.Result.Should().BeOfType<NotFoundObjectResult>();
        }

        [Fact]
        public async Task GetNotification_Found_ReturnsOk()
        {
            var dto = new NotificationResponseDto { NotificationId = Guid.NewGuid(), Title = "T" };
            var notificationServiceMock = new Mock<INotificationService>();
            var connectionManagerMock = new Mock<NotificationConnectionManager>();
            notificationServiceMock.Setup(s => s.GetNotificationByIdAsync(dto.NotificationId)).ReturnsAsync(dto);

            var controller = new NotificationController(notificationServiceMock.Object, connectionManagerMock.Object);
            var ctx = new DefaultHttpContext();
            ctx.User = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString()) }));
            controller.ControllerContext = new ControllerContext { HttpContext = ctx };

            var result = await controller.GetNotification(dto.NotificationId);
            result.Result.Should().BeOfType<OkObjectResult>();
            var ok = result.Result as OkObjectResult;
            ok.Value.Should().BeEquivalentTo(dto);
        }

        [Fact]
        public async Task HealthCheck_NoProvider_Returns500()
        {
            var notificationServiceMock = new Mock<INotificationService>();
            var connectionManagerMock = new Mock<NotificationConnectionManager>();

            var controller = new NotificationController(notificationServiceMock.Object, connectionManagerMock.Object);
            var ctx = new DefaultHttpContext();
            // No IPubSubNotificationProvider in services
            ctx.RequestServices = new ServiceCollection().BuildServiceProvider();
            controller.ControllerContext = new ControllerContext { HttpContext = ctx };

            var result = await controller.HealthCheck();
            var status = result as ObjectResult;
            status.StatusCode.Should().Be(500);
        }

        [Fact]
        public async Task HealthCheck_WithProvider_ReturnsOk()
        {
            var notificationServiceMock = new Mock<INotificationService>();
            var connectionManagerMock = new Mock<NotificationConnectionManager>();

            var pubsubMock = new Mock<IPubSubNotificationProvider>();
            pubsubMock.Setup(p => p.TopicExistsAsync(It.IsAny<string>())).ReturnsAsync(true);

            var services = new ServiceCollection();
            services.AddSingleton<IPubSubNotificationProvider>(pubsubMock.Object);
            var sp = services.BuildServiceProvider();

            var controller = new NotificationController(notificationServiceMock.Object, connectionManagerMock.Object);
            var ctx = new DefaultHttpContext();
            ctx.RequestServices = sp;
            controller.ControllerContext = new ControllerContext { HttpContext = ctx };

            var result = await controller.HealthCheck();
            result.Should().BeOfType<OkObjectResult>();
        }

        [Fact]
        public async Task TestPublishNotification_NoProvider_Returns500()
        {
            var notificationServiceMock = new Mock<INotificationService>();
            var connectionManagerMock = new Mock<NotificationConnectionManager>();

            var controller = new NotificationController(notificationServiceMock.Object, connectionManagerMock.Object);
            var ctx = new DefaultHttpContext();
            ctx.RequestServices = new ServiceCollection().BuildServiceProvider();
            controller.ControllerContext = new ControllerContext { HttpContext = ctx };

            var req = new CreateTestNotificationRequest { Title = "x" };
            var result = await controller.TestPublishNotification(req);
            var obj = result as ObjectResult;
            obj.StatusCode.Should().Be(500);
        }

        [Fact]
        public async Task TestPublishNotification_WithProvider_Publishes()
        {
            var notificationServiceMock = new Mock<INotificationService>();
            var connectionManagerMock = new Mock<NotificationConnectionManager>();

            var pubsubMock = new Mock<IPubSubNotificationProvider>();
            pubsubMock.Setup(p => p.PublishNotificationAsync(It.IsAny<NotificationResponseDto>(), It.IsAny<string>())).Returns(Task.CompletedTask);

            var services = new ServiceCollection();
            services.AddSingleton<IPubSubNotificationProvider>(pubsubMock.Object);
            var sp = services.BuildServiceProvider();

            var controller = new NotificationController(notificationServiceMock.Object, connectionManagerMock.Object);
            var ctx = new DefaultHttpContext();
            ctx.RequestServices = sp;
            controller.ControllerContext = new ControllerContext { HttpContext = ctx };

            var req = new CreateTestNotificationRequest { Title = "x" };
            var result = await controller.TestPublishNotification(req);
            result.Should().BeOfType<OkObjectResult>();
        }
    }
}
