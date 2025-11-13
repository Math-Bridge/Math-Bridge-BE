using FluentAssertions;
using MathBridgeSystem.Api.Controllers;
using MathBridgeSystem.Application.DTOs;
using MathBridgeSystem.Application.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using System.Security.Claims;
using Xunit;

namespace MathBridgeSystem.Test.Controllers
{
    public class ParentChildrenControllerTests
    {
        private readonly Mock<IChildService> _childServiceMock;
        private readonly ParentChildrenController _controller;

        public ParentChildrenControllerTests()
        {
            _childServiceMock = new Mock<IChildService>();
            _controller = new ParentChildrenController(_childServiceMock.Object);
            SetupUserClaims(Guid.NewGuid());
        }

        #region Constructor Tests

        [Fact]
        public void Constructor_NullChildService_ThrowsArgumentNullException()
        {
            Action act = () => new ParentChildrenController(null!);
            act.Should().Throw<ArgumentNullException>().WithParameterName("childService");
        }

        #endregion

        #region AddChild Tests

        [Fact]
        public async Task AddChild_ValidRequest_ReturnsOkWithChildId()
        {
            // Arrange
            var parentId = Guid.NewGuid();
            var request = new AddChildRequest
            {
                FullName = "Child Name",
                DateOfBirth = DateOnly.FromDateTime(DateTime.Now.AddYears(-8)),
                Grade = "Grade 3",
                SchoolId = Guid.NewGuid()
            };
            var expectedChildId = Guid.NewGuid();
            _childServiceMock.Setup(s => s.AddChildAsync(parentId, request))
                .ReturnsAsync(expectedChildId);

            // Act
            var result = await _controller.AddChild(parentId, request);

            // Assert
            result.Should().BeOfType<OkObjectResult>();
            _childServiceMock.Verify(s => s.AddChildAsync(parentId, request), Times.Once);
        }

        [Fact]
        public async Task AddChild_ServiceThrowsException_ReturnsInternalServerError()
        {
            // Arrange
            var parentId = Guid.NewGuid();
            var request = new AddChildRequest
            {
                FullName = "Child Name",
                DateOfBirth = DateOnly.FromDateTime(DateTime.Now.AddYears(-8)),
                Grade = "Grade 3",
                SchoolId = Guid.NewGuid()
            };
            _childServiceMock.Setup(s => s.AddChildAsync(parentId, request))
                .ThrowsAsync(new Exception("Database error"));

            // Act
            var result = await _controller.AddChild(parentId, request);

            // Assert
            var statusResult = result.Should().BeOfType<ObjectResult>().Subject;
            statusResult.StatusCode.Should().Be(500);
        }

        #endregion

        #region GetChildren Tests

        [Fact]
        public async Task GetChildren_ValidParentId_ReturnsOkWithChildren()
        {
            // Arrange
            var parentId = Guid.NewGuid();
            var expectedChildren = new List<ChildDto>
            {
                new ChildDto { ChildId = Guid.NewGuid(), FullName = "Child 1" },
                new ChildDto { ChildId = Guid.NewGuid(), FullName = "Child 2" }
            };
            _childServiceMock.Setup(s => s.GetChildrenByParentAsync(parentId))
                .ReturnsAsync(expectedChildren);

            // Act
            var result = await _controller.GetChildren(parentId);

            // Assert
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            okResult.Value.Should().BeEquivalentTo(expectedChildren);
            _childServiceMock.Verify(s => s.GetChildrenByParentAsync(parentId), Times.Once);
        }

        [Fact]
        public async Task GetChildren_EmptyList_ReturnsOkWithEmptyList()
        {
            // Arrange
            var parentId = Guid.NewGuid();
            _childServiceMock.Setup(s => s.GetChildrenByParentAsync(parentId))
                .ReturnsAsync(new List<ChildDto>());

            // Act
            var result = await _controller.GetChildren(parentId);

            // Assert
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            var children = okResult.Value as List<ChildDto>;
            children.Should().NotBeNull();
            children.Should().BeEmpty();
        }

        [Fact]
        public async Task GetChildren_ServiceThrowsException_ReturnsInternalServerError()
        {
            // Arrange
            var parentId = Guid.NewGuid();
            _childServiceMock.Setup(s => s.GetChildrenByParentAsync(parentId))
                .ThrowsAsync(new Exception("Database error"));

            // Act
            var result = await _controller.GetChildren(parentId);

            // Assert
            var statusResult = result.Should().BeOfType<ObjectResult>().Subject;
            statusResult.StatusCode.Should().Be(500);
        }

        #endregion

        #region GetMyChildren Tests

        [Fact]
        public async Task GetMyChildren_ValidUser_ReturnsOkWithChildren()
        {
            // Arrange
            var currentUserId = Guid.NewGuid();
            SetupUserClaims(currentUserId);
            var expectedChildren = new List<ChildDto>
            {
                new ChildDto { ChildId = Guid.NewGuid(), FullName = "My Child 1" },
                new ChildDto { ChildId = Guid.NewGuid(), FullName = "My Child 2" }
            };
            _childServiceMock.Setup(s => s.GetChildrenByParentAsync(currentUserId))
                .ReturnsAsync(expectedChildren);

            // Act
            var result = await _controller.GetMyChildren();

            // Assert
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            okResult.Value.Should().BeEquivalentTo(expectedChildren);
            _childServiceMock.Verify(s => s.GetChildrenByParentAsync(currentUserId), Times.Once);
        }

        [Fact]
        public async Task GetMyChildren_InvalidToken_ReturnsInternalServerError()
        {
            // Arrange
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity())
                }
            };

            // Act
            var result = await _controller.GetMyChildren();

            // Assert
            var statusResult = result.Should().BeOfType<ObjectResult>().Subject;
            statusResult.StatusCode.Should().Be(500);
        }

        #endregion

        #region Helper Methods

        private void SetupUserClaims(Guid userId)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
                new Claim(ClaimTypes.Role, "parent")
            };
            var identity = new ClaimsIdentity(claims, "TestAuthType");
            var claimsPrincipal = new ClaimsPrincipal(identity);

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = claimsPrincipal }
            };
        }

        #endregion
    }
}

