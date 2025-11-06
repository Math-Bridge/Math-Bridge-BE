using FluentAssertions;
using MathBridgeSystem.Application.DTOs;
using MathBridgeSystem.Application.Interfaces;
using MathBridgeSystem.Application.Services;
using MathBridgeSystem.Domain.Entities;
using MathBridgeSystem.Domain.Interfaces;
using MathBridgeSystem.Infrastructure.Data;
using Moq;
using MathBridgeSystem.Tests.Helpers;
using Xunit;

namespace MathBridgeSystem.Tests.Services
{
    public class CenterServiceTests
    {
        private readonly Mock<ICenterRepository> _centerRepositoryMock;
        private readonly Mock<ITutorCenterRepository> _tutorCenterRepositoryMock;
        private readonly Mock<IUserRepository> _userRepositoryMock;
        private readonly Mock<MathBridgeDbContext> _contextMock;
        private readonly Mock<IGoogleMapsService> _googleMapsServiceMock;
        private readonly CenterService _centerService;
        private readonly List<Contract> _contracts = new();
        private readonly List<Child> _children = new();

        public CenterServiceTests()
        {
            _centerRepositoryMock = new Mock<ICenterRepository>();
            _tutorCenterRepositoryMock = new Mock<ITutorCenterRepository>();
            _userRepositoryMock = new Mock<IUserRepository>();
            _contextMock = new Mock<MathBridgeDbContext>();
            _googleMapsServiceMock = new Mock<IGoogleMapsService>();
            _centerService = new CenterService(
                _centerRepositoryMock.Object,
                _tutorCenterRepositoryMock.Object,
                _userRepositoryMock.Object,
                _contextMock.Object,
                _googleMapsServiceMock.Object
            );
            _contextMock.Setup(c => c.Contracts).Returns(() => _contracts.AsQueryable().BuildMockDbSet().Object);
            _contextMock.Setup(c => c.Children).Returns(() => _children.AsQueryable().BuildMockDbSet().Object);
        }

        [Fact]
        public async Task CreateCenterAsync_ValidRequest_ReturnsCenterId()
        {
            // Arrange
            var request = new CreateCenterRequest { Name = "New Center", PlaceId = "valid-place-id" };
            var placeDetailsResponse = new PlaceDetailsResponse
            {
                Success = true,
                Place = new PlaceDetails
                {
                    FormattedAddress = "Test Address",
                    Latitude = 10.0,
                    Longitude = 106.0,
                    City = "Ho Chi Minh",
                    District = "District 1",
                    PlaceName = "Test Place",
                    CountryCode = "VN"
                }
            };
            _googleMapsServiceMock.Setup(service => service.GetPlaceDetailsAsync(request.PlaceId)).ReturnsAsync(placeDetailsResponse);
            _centerRepositoryMock.Setup(repo => repo.GetAllAsync()).ReturnsAsync(new List<Center>());
            _centerRepositoryMock.Setup(repo => repo.AddAsync(It.IsAny<Center>())).Returns(Task.CompletedTask);

            // Act
            var result = await _centerService.CreateCenterAsync(request);

            // Assert
            result.Should().NotBe(Guid.Empty);
            _centerRepositoryMock.Verify(repo => repo.AddAsync(It.IsAny<Center>()), Times.Once);
        }

        [Fact]
        public async Task CreateCenterAsync_DuplicateCenter_ThrowsException()
        {
            // Arrange
            var request = new CreateCenterRequest { Name = "Existing Center", PlaceId = "valid-place-id" };
            var placeDetailsResponse = new PlaceDetailsResponse { Success = true, Place = new PlaceDetails { City = "City", District = "District" } };
            var existingCenters = new List<Center> { new Center { Name = "Existing Center", City = "City", District = "District" } };
            _googleMapsServiceMock.Setup(service => service.GetPlaceDetailsAsync(It.IsAny<string>())).ReturnsAsync(placeDetailsResponse);
            _centerRepositoryMock.Setup(repo => repo.GetAllAsync()).ReturnsAsync(existingCenters);

            // Act & Assert
            Func<Task> act = () => _centerService.CreateCenterAsync(request);
            await act.Should().ThrowAsync<Exception>().WithMessage("*already exists*");
        }

        [Fact]
        public async Task UpdateCenterAsync_ValidRequest_UpdatesCenter()
        {
            // Arrange
            var id = Guid.NewGuid();
            var request = new UpdateCenterRequest { Name = "Updated Name", PlaceId = "new-place-id" };
            var existingCenter = new Center { CenterId = id, Name = "Old Name", GooglePlaceId = "old-place-id", City = "Old City", District = "Old District" };
            var placeDetailsResponse = new PlaceDetailsResponse { Success = true, Place = new PlaceDetails { City = "New City", District = "New District" } };
            _centerRepositoryMock.Setup(repo => repo.GetByIdAsync(id)).ReturnsAsync(existingCenter);
            _googleMapsServiceMock.Setup(service => service.GetPlaceDetailsAsync(It.IsAny<string>())).ReturnsAsync(placeDetailsResponse);
            _centerRepositoryMock.Setup(repo => repo.GetAllAsync()).ReturnsAsync(new List<Center>());
            _centerRepositoryMock.Setup(repo => repo.UpdateAsync(It.IsAny<Center>())).Returns(Task.CompletedTask);

            // Act
            await _centerService.UpdateCenterAsync(id, request);

            // Assert
            _centerRepositoryMock.Verify(repo => repo.UpdateAsync(It.Is<Center>(c => c.Name == request.Name && c.City == "New City")), Times.Once);
        }

        [Fact]
        public async Task UpdateCenterAsync_CenterNotFound_ThrowsException()
        {
            // Arrange
            var id = Guid.NewGuid();
            var request = new UpdateCenterRequest();
            _centerRepositoryMock.Setup(repo => repo.GetByIdAsync(id)).ReturnsAsync((Center)null);

            // Act & Assert
            Func<Task> act = () => _centerService.UpdateCenterAsync(id, request);
            await act.Should().ThrowAsync<Exception>().WithMessage("Center not found");
        }

        [Fact]
        public async Task DeleteCenterAsync_NoDependencies_DeletesCenter()
        {
            var id = Guid.NewGuid();
            var center = new Center { CenterId = id };
            _centerRepositoryMock.Setup(repo => repo.GetByIdAsync(id)).ReturnsAsync(center);
            _tutorCenterRepositoryMock.Setup(repo => repo.GetByCenterIdAsync(id)).ReturnsAsync(new List<TutorCenter>());

            // Dùng List rỗng
            _contracts.Clear();
            _children.Clear();

            _centerRepositoryMock.Setup(repo => repo.DeleteAsync(id)).Returns(Task.CompletedTask);

            await _centerService.DeleteCenterAsync(id);

            _centerRepositoryMock.Verify(repo => repo.DeleteAsync(id), Times.Once);
        }

        [Fact]
        public async Task DeleteCenterAsync_HasActiveContracts_ThrowsException()
        {
            var id = Guid.NewGuid();
            var center = new Center { CenterId = id };
            _centerRepositoryMock.Setup(repo => repo.GetByIdAsync(id)).ReturnsAsync(center);

            _contracts.Add(new Contract()); // Có contract

            var act = () => _centerService.DeleteCenterAsync(id);
            await act.Should().ThrowAsync<Exception>().WithMessage("Cannot delete center with active contracts");
        }

        [Fact]
        public async Task GetCenterByIdAsync_ExistingCenter_ReturnsDto()
        {
            // Arrange
            var id = Guid.NewGuid();
            var center = new Center { CenterId = id, Name = "Test Center" };
            _centerRepositoryMock.Setup(repo => repo.GetByIdAsync(id)).ReturnsAsync(center);

            // Act
            var result = await _centerService.GetCenterByIdAsync(id);

            // Assert
            result.CenterId.Should().Be(id);
            result.Name.Should().Be("Test Center");
        }

        [Fact]
        public async Task GetCenterByIdAsync_NonExisting_ThrowsException()
        {
            // Arrange
            var id = Guid.NewGuid();
            _centerRepositoryMock.Setup(repo => repo.GetByIdAsync(id)).ReturnsAsync((Center)null);

            // Act & Assert
            Func<Task> act = () => _centerService.GetCenterByIdAsync(id);
            await act.Should().ThrowAsync<Exception>().WithMessage("Center not found");
        }

        [Fact]
        public async Task GetAllCentersAsync_ReturnsList()
        {
            // Arrange
            _centerRepositoryMock.Setup(repo => repo.GetAllAsync()).ReturnsAsync(new List<Center> { new Center(), new Center() });

            // Act
            var result = await _centerService.GetAllCentersAsync();

            // Assert
            result.Should().HaveCount(2);
        }

        [Fact]
        public async Task GetCentersWithTutorsAsync_ReturnsListWithTutors()
        {
            // Arrange
            var centers = new List<Center> { new Center { CenterId = Guid.NewGuid() } };
            _centerRepositoryMock.Setup(repo => repo.GetAllAsync()).ReturnsAsync(centers);
            _userRepositoryMock.Setup(repo => repo.GetTutorsByCenterAsync(It.IsAny<Guid>())).ReturnsAsync(new List<User> { new User { TutorVerification = new TutorVerification() } });

            // Act
            var result = await _centerService.GetCentersWithTutorsAsync();

            // Assert
            result.Should().HaveCount(1);
            result[0].Tutors.Should().HaveCount(1);
        }

        [Fact]
        public async Task SearchCentersAsync_WithFilters_ReturnsFilteredList()
        {
            // Arrange
            var request = new CenterSearchRequest { City = "TestCity", Page = 1, PageSize = 10 };
            _centerRepositoryMock.Setup(repo => repo.GetAllAsync()).ReturnsAsync(new List<Center> { new Center { City = "TestCity" }, new Center { City = "Other" } });

            // Act
            var result = await _centerService.SearchCentersAsync(request);

            // Assert
            result.Should().HaveCount(1);
            result[0].City.Should().Be("TestCity");
        }

        [Fact]
        public async Task GetCentersCountByCriteriaAsync_ReturnsCount()
        {
            // Arrange
            var request = new CenterSearchRequest { City = "TestCity" };
            _centerRepositoryMock.Setup(repo => repo.GetAllAsync()).ReturnsAsync(new List<Center> { new Center { City = "TestCity" } });

            // Act
            var result = await _centerService.GetCentersCountByCriteriaAsync(request);

            // Assert
            result.Should().Be(1);
        }

        [Fact]
        public async Task GetCentersByCityAsync_ValidCity_ReturnsList()
        {
            // Arrange
            var city = "TestCity";
            _centerRepositoryMock.Setup(repo => repo.GetByCityAsync(city)).ReturnsAsync(new List<Center> { new Center() });

            // Act
            var result = await _centerService.GetCentersByCityAsync(city);

            // Assert
            result.Should().HaveCount(1);
        }

        [Fact]
        public async Task GetCentersNearLocationAsync_ValidCoordinates_ReturnsList()
        {
            // Arrange
            double lat = 10, lon = 20, radius = 10;
            _centerRepositoryMock.Setup(repo => repo.GetByCoordinates(lat, lon, radius)).ReturnsAsync(new List<Center> { new Center() });

            // Act
            var result = await _centerService.GetCentersNearLocationAsync(lat, lon, radius);

            // Assert
            result.Should().HaveCount(1);
        }

        [Fact]
        public async Task AssignTutorToCenterAsync_ValidAssignment_AssignsTutor()
        {
            // Arrange
            var centerId = Guid.NewGuid();
            var tutorId = Guid.NewGuid();
            var center = new Center();
            var tutor = new User { TutorVerification = new TutorVerification { VerificationStatus = "approved" } };
            _centerRepositoryMock.Setup(repo => repo.GetByIdAsync(centerId)).ReturnsAsync(center);
            _userRepositoryMock.Setup(repo => repo.GetTutorWithVerificationAsync(tutorId)).ReturnsAsync(tutor);
            _tutorCenterRepositoryMock.Setup(repo => repo.TutorIsAssignedToCenterAsync(tutorId, centerId)).ReturnsAsync(false);
            _tutorCenterRepositoryMock.Setup(repo => repo.AddAsync(It.IsAny<TutorCenter>())).Returns(Task.CompletedTask);
            _centerRepositoryMock.Setup(repo => repo.UpdateTutorCountAsync(centerId, 1)).Returns(Task.CompletedTask);

            // Act
            await _centerService.AssignTutorToCenterAsync(centerId, tutorId);

            // Assert
            _tutorCenterRepositoryMock.Verify(repo => repo.AddAsync(It.IsAny<TutorCenter>()), Times.Once);
        }

        [Fact]
        public async Task AssignTutorToCenterAsync_UnverifiedTutor_ThrowsException()
        {
            // Arrange
            var centerId = Guid.NewGuid();
            var tutorId = Guid.NewGuid();
            var center = new Center();
            var tutor = new User { TutorVerification = new TutorVerification { VerificationStatus = "pending" } };
            _centerRepositoryMock.Setup(repo => repo.GetByIdAsync(centerId)).ReturnsAsync(center);
            _userRepositoryMock.Setup(repo => repo.GetTutorWithVerificationAsync(tutorId)).ReturnsAsync(tutor);

            // Act & Assert
            Func<Task> act = () => _centerService.AssignTutorToCenterAsync(centerId, tutorId);
            await act.Should().ThrowAsync<Exception>().WithMessage("Tutor must be verified*");
        }

        [Fact]
        public async Task RemoveTutorFromCenterAsync_ValidRemoval_RemovesTutor()
        {
            var centerId = Guid.NewGuid();
            var tutorId = Guid.NewGuid();
            var tutorCenterId = Guid.NewGuid();
            var center = new Center();
            var tutor = new User();
            var tutorCenter = new TutorCenter { TutorCenterId = tutorCenterId, CenterId = centerId, TutorId = tutorId };

            _centerRepositoryMock.Setup(repo => repo.GetByIdAsync(centerId)).ReturnsAsync(center);
            _userRepositoryMock.Setup(repo => repo.GetTutorWithVerificationAsync(tutorId)).ReturnsAsync(tutor);
            _tutorCenterRepositoryMock.Setup(repo => repo.TutorIsAssignedToCenterAsync(tutorId, centerId)).ReturnsAsync(true);
            _tutorCenterRepositoryMock.Setup(repo => repo.GetByTutorIdAsync(tutorId)).ReturnsAsync(new List<TutorCenter> { tutorCenter });
            _tutorCenterRepositoryMock.Setup(repo => repo.RemoveAsync(tutorCenterId)).Returns(Task.CompletedTask);
            _centerRepositoryMock.Setup(repo => repo.UpdateTutorCountAsync(centerId, -1)).Returns(Task.CompletedTask);

            await _centerService.RemoveTutorFromCenterAsync(centerId, tutorId);

            _tutorCenterRepositoryMock.Verify(repo => repo.RemoveAsync(tutorCenterId), Times.Once);
        }

        [Fact]
        public async Task GetTutorsByCenterIdAsync_ValidCenter_ReturnsTutors()
        {
            // Arrange
            var centerId = Guid.NewGuid();
            var center = new Center();
            var tutors = new List<User> { new User { TutorVerification = new TutorVerification() } };
            _centerRepositoryMock.Setup(repo => repo.GetByIdAsync(centerId)).ReturnsAsync(center);
            _userRepositoryMock.Setup(repo => repo.GetTutorsByCenterAsync(centerId)).ReturnsAsync(tutors);

            // Act
            var result = await _centerService.GetTutorsByCenterIdAsync(centerId);

            // Assert
            result.Should().HaveCount(1);
        }
    }
}