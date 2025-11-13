using MathBridgeSystem.Api.Controllers;
using MathBridgeSystem.Application.Interfaces;
using MathBridgeSystem.Application.DTOs;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;
using FluentAssertions;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MathBridgeSystem.Tests.Controllers
{
    public class CenterControllerTests
    {
        [Fact]
        public async Task CreateCenter_InvalidModel_ReturnsBadRequest()
        {
            var centerServiceMock = new Mock<ICenterService>();
            var locationServiceMock = new Mock<ILocationService>();
            var ctrl = new CenterController(centerServiceMock.Object, locationServiceMock.Object);
            ctrl.ModelState.AddModelError("Name", "Required");

            var result = await ctrl.CreateCenter(new CreateCenterRequest());
            result.Should().BeOfType<BadRequestObjectResult>();
        }

        [Fact]
        public async Task CreateCenter_ServiceReturnsId_ReturnsCreated()
        {
            var id = Guid.NewGuid();
            var centerServiceMock = new Mock<ICenterService>();
            var locationServiceMock = new Mock<ILocationService>();
            centerServiceMock.Setup(s => s.CreateCenterAsync(It.IsAny<CreateCenterRequest>())).ReturnsAsync(id);

            var ctrl = new CenterController(centerServiceMock.Object, locationServiceMock.Object);

            var result = await ctrl.CreateCenter(new CreateCenterRequest { Name = "X", PlaceId = "p" });
            result.Should().BeOfType<CreatedAtActionResult>();
            var created = result as CreatedAtActionResult;
            created.Value.Should().NotBeNull();
        }

        [Fact]
        public async Task CreateCenter_AlreadyExists_ReturnsConflict()
        {
            var centerServiceMock = new Mock<ICenterService>();
            var locationServiceMock = new Mock<ILocationService>();
            centerServiceMock.Setup(s => s.CreateCenterAsync(It.IsAny<CreateCenterRequest>()))
                .ThrowsAsync(new Exception("center already exists"));

            var ctrl = new CenterController(centerServiceMock.Object, locationServiceMock.Object);

            var result = await ctrl.CreateCenter(new CreateCenterRequest { Name = "X", PlaceId = "p" });
            result.Should().BeOfType<ConflictObjectResult>();
        }

        [Fact]
        public async Task CreateCenter_GoogleMapsError_ReturnsBadRequest()
        {
            var centerServiceMock = new Mock<ICenterService>();
            var locationServiceMock = new Mock<ILocationService>();
            centerServiceMock.Setup(s => s.CreateCenterAsync(It.IsAny<CreateCenterRequest>()))
                .ThrowsAsync(new Exception("Google Maps error"));

            var ctrl = new CenterController(centerServiceMock.Object, locationServiceMock.Object);

            var result = await ctrl.CreateCenter(new CreateCenterRequest { Name = "X", PlaceId = "p" });
            result.Should().BeOfType<BadRequestObjectResult>();
        }

        [Fact]
        public async Task DeleteCenter_Success_ReturnsNoContent()
        {
            var centerServiceMock = new Mock<ICenterService>();
            var locationServiceMock = new Mock<ILocationService>();
            centerServiceMock.Setup(s => s.DeleteCenterAsync(It.IsAny<Guid>())).Returns(Task.CompletedTask);

            var ctrl = new CenterController(centerServiceMock.Object, locationServiceMock.Object);

            var result = await ctrl.DeleteCenter(Guid.NewGuid());
            result.Should().BeOfType<NoContentResult>();
        }

        [Fact]
        public async Task DeleteCenter_CannotDelete_ReturnsBadRequest()
        {
            var centerServiceMock = new Mock<ICenterService>();
            var locationServiceMock = new Mock<ILocationService>();
            centerServiceMock.Setup(s => s.DeleteCenterAsync(It.IsAny<Guid>()))
                .ThrowsAsync(new Exception("Cannot delete center because it has active contracts"));

            var ctrl = new CenterController(centerServiceMock.Object, locationServiceMock.Object);

            var result = await ctrl.DeleteCenter(Guid.NewGuid());
            result.Should().BeOfType<BadRequestObjectResult>();
        }

        [Fact]
        public async Task GetCenterById_ReturnsOkOrNotFound()
        {
            var centerServiceMock = new Mock<ICenterService>();
            var locationServiceMock = new Mock<ILocationService>();

            centerServiceMock.Setup(s => s.GetCenterByIdAsync(It.IsAny<Guid>())).ReturnsAsync(new CenterDto { CenterId = Guid.NewGuid(), Name = "C" });
            var ctrl = new CenterController(centerServiceMock.Object, locationServiceMock.Object);

            var okResult = await ctrl.GetCenterById(Guid.NewGuid());
            okResult.Should().BeOfType<OkObjectResult>();

            centerServiceMock.Setup(s => s.GetCenterByIdAsync(It.IsAny<Guid>())).ThrowsAsync(new Exception("not found"));
            var notFoundResult = await ctrl.GetCenterById(Guid.NewGuid());
            notFoundResult.Should().BeOfType<NotFoundObjectResult>();
        }

        [Fact]
        public async Task GetAllCenters_PaginatesAndReturnsOk()
        {
            var centerServiceMock = new Mock<ICenterService>();
            var locationServiceMock = new Mock<ILocationService>();

            var list = new List<CenterDto>
            {
                new CenterDto { CenterId = Guid.NewGuid(), Name = "A", City = "Alpha" },
                new CenterDto { CenterId = Guid.NewGuid(), Name = "B", City = "Beta" }
            };
            centerServiceMock.Setup(s => s.GetAllCentersAsync()).ReturnsAsync(list);

            var ctrl = new CenterController(centerServiceMock.Object, locationServiceMock.Object);

            var result = await ctrl.GetAllCenters(page: 1, pageSize: 1);
            result.Should().BeOfType<OkObjectResult>();
            var ok = result as OkObjectResult;
            ok.Value.Should().NotBeNull();
        }

        [Fact]
        public async Task SearchCenters_ReturnsOk()
        {
            var centerServiceMock = new Mock<ICenterService>();
            var locationServiceMock = new Mock<ILocationService>();

            centerServiceMock.Setup(s => s.SearchCentersAsync(It.IsAny<CenterSearchRequest>())).ReturnsAsync(new List<CenterDto> { new CenterDto { CenterId = Guid.NewGuid(), Name = "X" } });
            centerServiceMock.Setup(s => s.GetCentersCountByCriteriaAsync(It.IsAny<CenterSearchRequest>())).ReturnsAsync(1);

            var ctrl = new CenterController(centerServiceMock.Object, locationServiceMock.Object);

            var result = await ctrl.SearchCenters(name: "x", page: 1, pageSize: 10);
            result.Should().BeOfType<OkObjectResult>();
        }
    }
}
