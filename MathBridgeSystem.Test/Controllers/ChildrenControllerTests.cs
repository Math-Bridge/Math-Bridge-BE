using MathBridgeSystem.Api.Controllers;
using MathBridgeSystem.Application.DTOs;
using MathBridgeSystem.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;
using Assert = Xunit.Assert;

namespace MathBridgeSystem.Test.Controllers
{
    public class ChildrenControllerTests
    {
        private readonly Mock<IChildService> _mockChildService;
        private readonly ChildrenController _controller;

        public ChildrenControllerTests()
        {
            _mockChildService = new Mock<IChildService>();
            _controller = new ChildrenController(_mockChildService.Object);
        }

        [Fact]
        public async Task UpdateChild_ValidRequest_ReturnsOk()
        {
            // Arrange
            var childId = Guid.NewGuid();
            var request = new UpdateChildRequest { FullName = "Updated Name" };
            _mockChildService.Setup(s => s.UpdateChildAsync(childId, request)).Returns(Task.CompletedTask);

            // Act
            var result = await _controller.UpdateChild(childId, request);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.NotNull(okResult.Value);
            _mockChildService.Verify(s => s.UpdateChildAsync(childId, request), Times.Once);
        }

        [Fact]
        public async Task UpdateChild_InvalidModelState_ReturnsBadRequest()
        {
            // Arrange
            var childId = Guid.NewGuid();
            var request = new UpdateChildRequest();
            _controller.ModelState.AddModelError("FullName", "FullName is required");

            // Act
            var result = await _controller.UpdateChild(childId, request);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.IsType<SerializableError>(badRequestResult.Value);
            _mockChildService.Verify(s => s.UpdateChildAsync(It.IsAny<Guid>(), It.IsAny<UpdateChildRequest>()), Times.Never);
        }

        [Fact]
        public async Task UpdateChild_ServiceThrowsException_ReturnsInternalServerError()
        {
            // Arrange
            var childId = Guid.NewGuid();
            var request = new UpdateChildRequest { FullName = "Updated Name" };
            _mockChildService.Setup(s => s.UpdateChildAsync(childId, request)).ThrowsAsync(new Exception("Test exception"));

            // Act
            var result = await _controller.UpdateChild(childId, request);

            // Assert
            var statusCodeResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(500, statusCodeResult.StatusCode);
        }

        [Fact]
        public async Task SoftDeleteChild_ValidId_ReturnsOk()
        {
            // Arrange
            var childId = Guid.NewGuid();
            _mockChildService.Setup(s => s.SoftDeleteChildAsync(childId)).Returns(Task.CompletedTask);

            // Act
            var result = await _controller.SoftDeleteChild(childId);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.NotNull(okResult.Value);
            _mockChildService.Verify(s => s.SoftDeleteChildAsync(childId), Times.Once);
        }

        [Fact]
        public async Task SoftDeleteChild_ServiceThrowsException_ReturnsInternalServerError()
        {
            // Arrange
            var childId = Guid.NewGuid();
            _mockChildService.Setup(s => s.SoftDeleteChildAsync(childId)).ThrowsAsync(new Exception("Test exception"));

            // Act
            var result = await _controller.SoftDeleteChild(childId);

            // Assert
            var statusCodeResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(500, statusCodeResult.StatusCode);
        }

        [Fact]
        public async Task GetChildById_ValidId_ReturnsOkWithChild()
        {
            // Arrange
            var childId = Guid.NewGuid();
            var childDto = new ChildDto { ChildId = childId, FullName = "Test Child" };
            _mockChildService.Setup(s => s.GetChildByIdAsync(childId)).ReturnsAsync(childDto);

            // Act
            var result = await _controller.GetChildById(childId);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var returnedChild = Assert.IsType<ChildDto>(okResult.Value);
            Assert.Equal(childId, returnedChild.ChildId);
            _mockChildService.Verify(s => s.GetChildByIdAsync(childId), Times.Once);
        }

        [Fact]
        public async Task GetChildById_ServiceThrowsException_ReturnsInternalServerError()
        {
            // Arrange
            var childId = Guid.NewGuid();
            _mockChildService.Setup(s => s.GetChildByIdAsync(childId)).ThrowsAsync(new Exception("Test exception"));

            // Act
            var result = await _controller.GetChildById(childId);

            // Assert
            var statusCodeResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(500, statusCodeResult.StatusCode);
        }

        [Fact]
        public async Task GetAllChildren_ReturnsOkWithChildren()
        {
            // Arrange
            var children = new List<ChildDto> { new ChildDto { ChildId = Guid.NewGuid(), FullName = "Test Child" } };
            _mockChildService.Setup(s => s.GetAllChildrenAsync()).ReturnsAsync(children);

            // Act
            var result = await _controller.GetAllChildren();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var returnedChildren = Assert.IsType<List<ChildDto>>(okResult.Value);
            Assert.Single(returnedChildren);
            _mockChildService.Verify(s => s.GetAllChildrenAsync(), Times.Once);
        }

        [Fact]
        public async Task GetAllChildren_ServiceThrowsException_ReturnsInternalServerError()
        {
            // Arrange
            _mockChildService.Setup(s => s.GetAllChildrenAsync()).ThrowsAsync(new Exception("Test exception"));

            // Act
            var result = await _controller.GetAllChildren();

            // Assert
            var statusCodeResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(500, statusCodeResult.StatusCode);
        }

        [Fact]
        public async Task LinkCenter_ValidRequest_ReturnsOk()
        {
            // Arrange
            var childId = Guid.NewGuid();
            var request = new LinkCenterRequest { CenterId = Guid.NewGuid() };
            _mockChildService.Setup(s => s.LinkCenterAsync(childId, request)).Returns(Task.CompletedTask);

            // Act
            var result = await _controller.LinkCenter(childId, request);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.NotNull(okResult.Value);
            _mockChildService.Verify(s => s.LinkCenterAsync(childId, request), Times.Once);
        }

        [Fact]
        public async Task LinkCenter_InvalidModelState_ReturnsBadRequest()
        {
            // Arrange
            var childId = Guid.NewGuid();
            var request = new LinkCenterRequest();
            _controller.ModelState.AddModelError("CenterId", "CenterId is required");

            // Act
            var result = await _controller.LinkCenter(childId, request);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.IsType<SerializableError>(badRequestResult.Value);
            _mockChildService.Verify(s => s.LinkCenterAsync(It.IsAny<Guid>(), It.IsAny<LinkCenterRequest>()), Times.Never);
        }

        [Fact]
        public async Task LinkCenter_ServiceThrowsException_ReturnsInternalServerError()
        {
            // Arrange
            var childId = Guid.NewGuid();
            var request = new LinkCenterRequest { CenterId = Guid.NewGuid() };
            _mockChildService.Setup(s => s.LinkCenterAsync(childId, request)).ThrowsAsync(new Exception("Test exception"));

            // Act
            var result = await _controller.LinkCenter(childId, request);

            // Assert
            var statusCodeResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(500, statusCodeResult.StatusCode);
        }

        [Fact]
        public async Task GetChildContracts_ValidId_ReturnsOkWithContracts()
        {
            // Arrange
            var childId = Guid.NewGuid();
            var contracts = new List<ContractDto> { new ContractDto { ChildId = Guid.NewGuid() } };
            _mockChildService.Setup(s => s.GetChildContractsAsync(childId)).ReturnsAsync(contracts);

            // Act
            var result = await _controller.GetChildContracts(childId);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var returnedContracts = Assert.IsType<List<ContractDto>>(okResult.Value);
            Assert.Single(returnedContracts);
            _mockChildService.Verify(s => s.GetChildContractsAsync(childId), Times.Once);
        }

        [Fact]
        public async Task GetChildContracts_ServiceThrowsException_ReturnsInternalServerError()
        {
            // Arrange
            var childId = Guid.NewGuid();
            _mockChildService.Setup(s => s.GetChildContractsAsync(childId)).ThrowsAsync(new Exception("Test exception"));

            // Act
            var result = await _controller.GetChildContracts(childId);

            // Assert
            var statusCodeResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(500, statusCodeResult.StatusCode);
        }

        [Fact]
        public async Task RestoreChild_ValidId_ReturnsOk()
        {
            // Arrange
            var childId = Guid.NewGuid();
            _mockChildService.Setup(s => s.RestoreChildAsync(childId)).Returns(Task.CompletedTask);

            // Act
            var result = await _controller.RestoreChild(childId);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.NotNull(okResult.Value);
            _mockChildService.Verify(s => s.RestoreChildAsync(childId), Times.Once);
        }

        [Fact]
        public async Task RestoreChild_ServiceThrowsException_ReturnsInternalServerError()
        {
            // Arrange
            var childId = Guid.NewGuid();
            _mockChildService.Setup(s => s.RestoreChildAsync(childId)).ThrowsAsync(new Exception("Test exception"));

            // Act
            var result = await _controller.RestoreChild(childId);

            // Assert
            var statusCodeResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(500, statusCodeResult.StatusCode);
        }
    }
}