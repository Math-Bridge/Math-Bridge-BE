using FluentAssertions;
using MathBridgeSystem.Application.DTOs;
using MathBridgeSystem.Application.Services;
using MathBridgeSystem.Domain.Entities;
using MathBridgeSystem.Domain.Interfaces;
using Moq;
using Xunit;

namespace MathBridgeSystem.Tests.Services
{
    public class SupportRequestServiceTests
    {
        private readonly Mock<ISupportRequestRepository> _supportRequestRepositoryMock;
        private readonly Mock<IUserRepository> _userRepositoryMock;
        private readonly SupportRequestService _service;

        public SupportRequestServiceTests()
        {
            _supportRequestRepositoryMock = new Mock<ISupportRequestRepository>();
            _userRepositoryMock = new Mock<IUserRepository>();

            _service = new SupportRequestService(
                _supportRequestRepositoryMock.Object,
                _userRepositoryMock.Object
            );
        }

        [Fact]
        public async Task CreateSupportRequestAsync_ShouldCreateRequest_WhenValid()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var request = new CreateSupportRequestRequest
            {
                Subject = "Need help",
                Description = "I have an issue",
                Category = "Technical"
            };

            _userRepositoryMock.Setup(r => r.ExistsAsync(userId))
                .ReturnsAsync(true);
            _supportRequestRepositoryMock.Setup(r => r.AddAsync(It.IsAny<SupportRequest>()))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _service.CreateSupportRequestAsync(request, userId);

            // Assert
            result.Should().NotBeEmpty();
            _supportRequestRepositoryMock.Verify(r => r.AddAsync(It.Is<SupportRequest>(
                sr => sr.Subject == "Need help" && sr.Status == "Open"
            )), Times.Once);
        }

        [Fact]
        public async Task CreateSupportRequestAsync_ShouldThrowArgumentException_WhenSubjectEmpty()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var request = new CreateSupportRequestRequest
            {
                Subject = "",
                Description = "Description",
                Category = "Technical"
            };

            // Act & Assert
            await Xunit.Assert.ThrowsAsync<ArgumentException>(
                async () => await _service.CreateSupportRequestAsync(request, userId)
            );
        }

        [Fact]
        public async Task CreateSupportRequestAsync_ShouldThrowInvalidOperationException_WhenUserNotFound()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var request = new CreateSupportRequestRequest
            {
                Subject = "Help",
                Description = "Description",
                Category = "Technical"
            };

            _userRepositoryMock.Setup(r => r.ExistsAsync(userId))
                .ReturnsAsync(false);

            // Act & Assert
            await Xunit.Assert.ThrowsAsync<InvalidOperationException>(
                async () => await _service.CreateSupportRequestAsync(request, userId)
            );
        }

        [Fact]
        public async Task UpdateSupportRequestAsync_ShouldUpdateRequest_WhenValid()
        {
            // Arrange
            var requestId = Guid.NewGuid();
            var userId = Guid.NewGuid();
            var supportRequest = new SupportRequest
            {
                RequestId = requestId,
                UserId = userId,
                Subject = "Old Subject",
                Description = "Old Description",
                Category = "Old Category"
            };

            var updateRequest = new UpdateSupportRequestRequest
            {
                Subject = "New Subject",
                Description = "New Description",
                Category = "New Category"
            };

            _supportRequestRepositoryMock.Setup(r => r.GetByIdAsync(requestId))
                .ReturnsAsync(supportRequest);

            // Act
            await _service.UpdateSupportRequestAsync(requestId, updateRequest, userId);

            // Assert
            supportRequest.Subject.Should().Be("New Subject");
            supportRequest.Description.Should().Be("New Description");
            supportRequest.Category.Should().Be("New Category");
            _supportRequestRepositoryMock.Verify(r => r.UpdateAsync(supportRequest), Times.Once);
        }

        [Fact]
        public async Task UpdateSupportRequestAsync_ShouldThrowUnauthorizedAccessException_WhenUserMismatch()
        {
            // Arrange
            var requestId = Guid.NewGuid();
            var userId = Guid.NewGuid();
            var differentUserId = Guid.NewGuid();
            var supportRequest = new SupportRequest
            {
                RequestId = requestId,
                UserId = differentUserId
            };

            var updateRequest = new UpdateSupportRequestRequest
            {
                Subject = "Subject",
                Description = "Description",
                Category = "Category"
            };

            _supportRequestRepositoryMock.Setup(r => r.GetByIdAsync(requestId))
                .ReturnsAsync(supportRequest);

            // Act & Assert
            await Xunit.Assert.ThrowsAsync<UnauthorizedAccessException>(
                async () => await _service.UpdateSupportRequestAsync(requestId, updateRequest, userId)
            );
        }

        [Fact]
        public async Task DeleteSupportRequestAsync_ShouldDeleteRequest_WhenValid()
        {
            // Arrange
            var requestId = Guid.NewGuid();
            var userId = Guid.NewGuid();
            var supportRequest = new SupportRequest
            {
                RequestId = requestId,
                UserId = userId
            };

            _supportRequestRepositoryMock.Setup(r => r.GetByIdAsync(requestId))
                .ReturnsAsync(supportRequest);

            // Act
            await _service.DeleteSupportRequestAsync(requestId, userId);

            // Assert
            _supportRequestRepositoryMock.Verify(r => r.DeleteAsync(requestId), Times.Once);
        }

        [Fact]
        public async Task GetSupportRequestByIdAsync_ShouldReturnRequest_WhenExists()
        {
            // Arrange
            var requestId = Guid.NewGuid();
            var supportRequest = new SupportRequest
            {
                RequestId = requestId,
                Subject = "Test",
                Description = "Test Description"
            };

            _supportRequestRepositoryMock.Setup(r => r.GetByIdAsync(requestId))
                .ReturnsAsync(supportRequest);

            // Act
            var result = await _service.GetSupportRequestByIdAsync(requestId);

            // Assert
            result.Should().NotBeNull();
            result!.RequestId.Should().Be(requestId);
        }

        [Fact]
        public async Task GetSupportRequestByIdAsync_ShouldReturnNull_WhenNotExists()
        {
            // Arrange
            var requestId = Guid.NewGuid();
            _supportRequestRepositoryMock.Setup(r => r.GetByIdAsync(requestId))
                .ReturnsAsync((SupportRequest)null!);

            // Act
            var result = await _service.GetSupportRequestByIdAsync(requestId);

            // Assert
            result.Should().BeNull();
        }

        [Fact]
        public void Constructor_ShouldThrowArgumentNullException_WhenSupportRequestRepositoryIsNull()
        {
            // Act & Assert
            var action = () => new SupportRequestService(null!, _userRepositoryMock.Object);
            action.Should().Throw<ArgumentNullException>();
        }
    }
}
