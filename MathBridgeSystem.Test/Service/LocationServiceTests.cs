using FluentAssertions;
using MathBridgeSystem.Application.DTOs;
using MathBridgeSystem.Application.Interfaces;
using MathBridgeSystem.Application.Services;
using MathBridgeSystem.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace MathBridgeSystem.Tests.Services
{
    public class LocationServiceTests
    {
        private readonly Mock<IGoogleMapsService> _googleMapsServiceMock;
        private readonly Mock<ICenterService> _centerServiceMock;
        private readonly Mock<IUserRepository> _userRepositoryMock;
        private readonly Mock<ILogger<LocationService>> _loggerMock;
        private readonly LocationService _locationService;

        public LocationServiceTests()
        {
            _googleMapsServiceMock = new Mock<IGoogleMapsService>();
            _centerServiceMock = new Mock<ICenterService>();
            _userRepositoryMock = new Mock<IUserRepository>();
            _loggerMock = new Mock<ILogger<LocationService>>();

            _locationService = new LocationService(
                _googleMapsServiceMock.Object,
                _centerServiceMock.Object,
                _userRepositoryMock.Object,
                _loggerMock.Object
            );
        }

        #region GetAddressAutocompleteAsync Tests

        // Test: Ném lỗi nếu input là null
        [Fact]
        public async Task GetAddressAutocompleteAsync_NullInput_ReturnsFailure()
        {
            var result = await _locationService.GetAddressAutocompleteAsync(null);

            result.Success.Should().BeFalse();
            result.ErrorMessage.Should().Be("Input is required for address autocomplete");
        }

        // Test: Ném lỗi nếu input là chuỗi rỗng
        [Fact]
        public async Task GetAddressAutocompleteAsync_EmptyInput_ReturnsFailure()
        {
            var result = await _locationService.GetAddressAutocompleteAsync("");

            result.Success.Should().BeFalse();
            result.ErrorMessage.Should().Be("Input is required for address autocomplete");
        }

        // Test: Trả về thành công khi Google Maps service trả về thành công
        [Fact]
        public async Task GetAddressAutocompleteAsync_ValidInput_ReturnsSuccessFromService()
        {
            var input = "123 Main St";
            var expectedResponse = new AddressAutocompleteResponse
            {
                Success = true,
                Predictions = new List<AddressPrediction> { new AddressPrediction { PlaceId = "1" } }
            };
            _googleMapsServiceMock.Setup(s => s.GetPlaceAutocompleteAsync(input, null)).ReturnsAsync(expectedResponse);

            var result = await _locationService.GetAddressAutocompleteAsync(input);

            result.Success.Should().BeTrue();
            result.Predictions.Should().HaveCount(1);
            _googleMapsServiceMock.Verify(s => s.GetPlaceAutocompleteAsync(input, null), Times.Once);
        }

        // Test: Trả về thất bại khi Google Maps service ném lỗi
        [Fact]
        public async Task GetAddressAutocompleteAsync_GoogleMapsServiceThrowsException_ReturnsFailure()
        {
            var input = "error input";
            _googleMapsServiceMock.Setup(s => s.GetPlaceAutocompleteAsync(input, null)).ThrowsAsync(new Exception("API Error"));

            var result = await _locationService.GetAddressAutocompleteAsync(input);

            result.Success.Should().BeFalse();
            result.ErrorMessage.Should().Be("Failed to get address suggestions");
        }

        #endregion

        #region SaveUserAddressAsync Tests

        // Test: Ném lỗi nếu Google Maps service thất bại khi lấy chi tiết địa điểm
        [Fact]
        public async Task SaveUserAddressAsync_GoogleMapsFails_ReturnsFailure()
        {
            var request = new SaveAddressRequest { PlaceId = "invalid-place-id" };
            var failedResponse = new PlaceDetailsResponse { Success = false, ErrorMessage = "Not Found" };
            _googleMapsServiceMock.Setup(s => s.GetPlaceDetailsAsync(request.PlaceId)).ReturnsAsync(failedResponse);

            var result = await _locationService.SaveUserAddressAsync(Guid.NewGuid(), request);

            result.Success.Should().BeFalse();
            result.Message.Should().Be("Failed to get place details from Google Maps");
        }

        // Test: Ném lỗi nếu Google Maps service trả về Place là null
        [Fact]
        public async Task SaveUserAddressAsync_GoogleMapsReturnsNullPlace_ReturnsFailure()
        {
            var request = new SaveAddressRequest { PlaceId = "valid-id-null-place" };
            var nullPlaceResponse = new PlaceDetailsResponse { Success = true, Place = null };
            _googleMapsServiceMock.Setup(s => s.GetPlaceDetailsAsync(request.PlaceId)).ReturnsAsync(nullPlaceResponse);

            var result = await _locationService.SaveUserAddressAsync(Guid.NewGuid(), request);

            result.Success.Should().BeFalse();
            result.Message.Should().Be("Failed to get place details from Google Maps");
        }

        // Test: Ném lỗi nếu không tìm thấy User
        [Fact]
        public async Task SaveUserAddressAsync_UserNotFound_ReturnsFailure()
        {
            var userId = Guid.NewGuid();
            var request = new SaveAddressRequest { PlaceId = "valid-place-id" };
            var placeDetails = new PlaceDetailsResponse { Success = true, Place = new PlaceDetails() };
            _googleMapsServiceMock.Setup(s => s.GetPlaceDetailsAsync(request.PlaceId)).ReturnsAsync(placeDetails);
            _userRepositoryMock.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync((Domain.Entities.User)null);

            var result = await _locationService.SaveUserAddressAsync(userId, request);

            result.Success.Should().BeFalse();
            result.Message.Should().Be("User not found");
        }

        // Test: Lưu địa chỉ thành công và cập nhật User
        [Fact]
        public async Task SaveUserAddressAsync_ValidRequest_UpdatesUserAndReturnsSuccess()
        {
            var userId = Guid.NewGuid();
            var request = new SaveAddressRequest { PlaceId = "valid-place-id" };
            var place = new PlaceDetails
            {
                FormattedAddress = "123 Main St, HCMC",
                Latitude = 10.0,
                Longitude = 106.0,
                City = "HCMC",
                District = "District 1",
                PlaceName = "Test Place",
                CountryCode = "VN"
            };
            var placeDetails = new PlaceDetailsResponse { Success = true, Place = place };
            var user = new Domain.Entities.User { UserId = userId };

            _googleMapsServiceMock.Setup(s => s.GetPlaceDetailsAsync(request.PlaceId)).ReturnsAsync(placeDetails);
            _userRepositoryMock.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync(user);
            _userRepositoryMock.Setup(r => r.UpdateAsync(It.IsAny<Domain.Entities.User>())).Returns(Task.CompletedTask);

            var result = await _locationService.SaveUserAddressAsync(userId, request);

            result.Success.Should().BeTrue();
            result.Message.Should().Be("Address saved successfully");

            user.FormattedAddress.Should().Be("123 Main St, HCMC");
            user.Latitude.Should().Be(10.0);
            user.City.Should().Be("HCMC");
            _userRepositoryMock.Verify(r => r.UpdateAsync(user), Times.Once);
        }

        // Test: Ném lỗi nếu UserRepository ném lỗi khi cập nhật
        [Fact]
        public async Task SaveUserAddressAsync_RepositoryThrowsException_ReturnsFailure()
        {
            var userId = Guid.NewGuid();
            var request = new SaveAddressRequest { PlaceId = "valid-place-id" };
            var placeDetails = new PlaceDetailsResponse { Success = true, Place = new PlaceDetails() };
            var user = new Domain.Entities.User { UserId = userId };

            _googleMapsServiceMock.Setup(s => s.GetPlaceDetailsAsync(request.PlaceId)).ReturnsAsync(placeDetails);
            _userRepositoryMock.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync(user);
            _userRepositoryMock.Setup(r => r.UpdateAsync(user)).ThrowsAsync(new Exception("DB Error"));

            var result = await _locationService.SaveUserAddressAsync(userId, request);

            result.Success.Should().BeFalse();
            result.Message.Should().Be("Failed to save address");
        }

        #endregion

        #region FindNearbyUsersAsync Tests

        // Test: Ném lỗi nếu không tìm thấy User hiện tại
        [Fact]
        public async Task FindNearbyUsersAsync_CurrentUserNotFound_ReturnsFailure()
        {
            var userId = Guid.NewGuid();
            _userRepositoryMock.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync((Domain.Entities.User)null);

            var result = await _locationService.FindNearbyUsersAsync(userId);

            result.Success.Should().BeFalse();
            result.ErrorMessage.Should().Be("Current user not found");
        }

        // Test: Ném lỗi nếu User hiện tại chưa có tọa độ
        [Fact]
        public async Task FindNearbyUsersAsync_CurrentUserHasNoLocation_ReturnsFailure()
        {
            var userId = Guid.NewGuid();
            var user = new Domain.Entities.User { UserId = userId, Latitude = null, Longitude = null };
            _userRepositoryMock.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync(user);

            var result = await _locationService.FindNearbyUsersAsync(userId);

            result.Success.Should().BeFalse();
            result.ErrorMessage.Should().Be("Current user location not set. Please save your address first.");
        }

        // Test: Tìm kiếm thành công (trả về danh sách đã lọc và sắp xếp)
        [Fact]
        public async Task FindNearbyUsersAsync_FindsNearbyUsers_ReturnsSuccessWithFilteredAndSortedList()
        {
            var currentUserId = Guid.NewGuid();
            var currentUser = new Domain.Entities.User { UserId = currentUserId, Latitude = 10.0, Longitude = 106.0 };

            var allUsers = new List<Domain.Entities.User>
            {
                currentUser,
                new Domain.Entities.User { UserId = Guid.NewGuid(), FullName = "User Far", Latitude = 11.0, Longitude = 107.0 },
                new Domain.Entities.User { UserId = Guid.NewGuid(), FullName = "User Close", Latitude = 10.01, Longitude = 106.01 },
                new Domain.Entities.User { UserId = Guid.NewGuid(), FullName = "User Closest", Latitude = 10.005, Longitude = 106.005 },
                new Domain.Entities.User { UserId = Guid.NewGuid(), FullName = "User No Location", Latitude = null, Longitude = null }
            };

            _userRepositoryMock.Setup(r => r.GetByIdAsync(currentUserId)).ReturnsAsync(currentUser);
            _userRepositoryMock.Setup(r => r.GetUsersWithLocationAsync()).ReturnsAsync(allUsers);

            var result = await _locationService.FindNearbyUsersAsync(currentUserId, radiusKm: 5);

            result.Success.Should().BeTrue();
            result.TotalUsers.Should().Be(2);
            result.NearbyUsers.Should().HaveCount(2);

            result.NearbyUsers[0].FullName.Should().Be("User Closest");


            result.NearbyUsers[0].DistanceKm.Should().Be(0.78);

            result.NearbyUsers[1].FullName.Should().Be("User Close");
            result.NearbyUsers[1].DistanceKm.Should().BeApproximately(1.56, 0.02);
        }

        // Test: Ném lỗi nếu Repository ném lỗi
        [Fact]
        public async Task FindNearbyUsersAsync_RepositoryThrowsException_ReturnsFailure()
        {
            var currentUserId = Guid.NewGuid();
            var currentUser = new Domain.Entities.User { UserId = currentUserId, Latitude = 10.0, Longitude = 106.0 };
            _userRepositoryMock.Setup(r => r.GetByIdAsync(currentUserId)).ReturnsAsync(currentUser);
            _userRepositoryMock.Setup(r => r.GetUsersWithLocationAsync()).ThrowsAsync(new Exception("DB Error"));

            var result = await _locationService.FindNearbyUsersAsync(currentUserId);

            result.Success.Should().BeFalse();
            result.ErrorMessage.Should().Be("Failed to find nearby users");
        }

        #endregion

        #region GeocodeAddressAsync Tests

        // Test: Ném lỗi nếu địa chỉ rỗng
        [Fact]
        public async Task GeocodeAddressAsync_NullOrEmptyAddress_ReturnsFailure()
        {
            var result = await _locationService.GeocodeAddressAsync(" ");

            result.Success.Should().BeFalse();
            result.ErrorMessage.Should().Be("Address is required");
        }

        // Test: Geocode thành công (chuyển tiếp từ GoogleMapsService)
        [Fact]
        public async Task GeocodeAddressAsync_ValidAddress_ReturnsSuccessFromService()
        {
            var address = "123 Main St";
            var expectedResponse = new GeocodeResponse { Success = true, Latitude = 10.0, Longitude = 106.0 };
            _googleMapsServiceMock.Setup(s => s.GeocodeAddressAsync(address, "VN")).ReturnsAsync(expectedResponse);

            var result = await _locationService.GeocodeAddressAsync(address);

            result.Success.Should().BeTrue();
            result.Latitude.Should().Be(10.0);
        }

        // Test: Ném lỗi nếu GoogleMapsService ném lỗi
        [Fact]
        public async Task GeocodeAddressAsync_GoogleMapsThrowsException_ReturnsFailure()
        {
            var address = "error address";
            _googleMapsServiceMock.Setup(s => s.GeocodeAddressAsync(address, "VN")).ThrowsAsync(new Exception("API Error"));

            var result = await _locationService.GeocodeAddressAsync(address);

            result.Success.Should().BeFalse();
            result.ErrorMessage.Should().Be("Failed to geocode address");
        }

        #endregion

        #region FindCentersNearAddressAsync Tests

        // Test: Trả về danh sách rỗng nếu Geocoding thất bại
        [Fact]
        public async Task FindCentersNearAddressAsync_GeocodingFails_ReturnsEmptyList()
        {
            var address = "invalid address";
            var geocodeResponse = new GeocodeResponse { Success = false, ErrorMessage = "Not Found" };
            _googleMapsServiceMock.Setup(s => s.GeocodeAddressAsync(address, "VN")).ReturnsAsync(geocodeResponse);

            var result = await _locationService.FindCentersNearAddressAsync(address);

            result.Should().BeEmpty();
            _centerServiceMock.Verify(c => c.GetCentersNearLocationAsync(It.IsAny<double>(), It.IsAny<double>(), It.IsAny<double>()), Times.Never);
        }

        // Test: Trả về danh sách trung tâm thành công
        [Fact]
        public async Task FindCentersNearAddressAsync_GeocodingSucceeds_ReturnsCentersFromService()
        {
            var address = "123 Main St";
            var geocodeResponse = new GeocodeResponse { Success = true, Latitude = 10.0, Longitude = 106.0 };
            var expectedCenters = new List<CenterDto> { new CenterDto { CenterId = Guid.NewGuid(), Name = "Nearby Center" } };

            _googleMapsServiceMock.Setup(s => s.GeocodeAddressAsync(address, "VN")).ReturnsAsync(geocodeResponse);
            _centerServiceMock.Setup(c => c.GetCentersNearLocationAsync(10.0, 106.0, 10.0)).ReturnsAsync(expectedCenters);

            var result = await _locationService.FindCentersNearAddressAsync(address, radiusKm: 10.0);

            result.Should().HaveCount(1);
            result.First().Name.Should().Be("Nearby Center");
        }

        // Test: Trả về danh sách rỗng nếu CenterService ném lỗi
        [Fact]
        public async Task FindCentersNearAddressAsync_CenterServiceThrowsException_ReturnsEmptyList()
        {
            var address = "123 Main St";
            var geocodeResponse = new GeocodeResponse { Success = true, Latitude = 10.0, Longitude = 106.0 };

            _googleMapsServiceMock.Setup(s => s.GeocodeAddressAsync(address, "VN")).ReturnsAsync(geocodeResponse);
            _centerServiceMock.Setup(c => c.GetCentersNearLocationAsync(10.0, 106.0, 10.0)).ThrowsAsync(new Exception("DB Error"));

            var result = await _locationService.FindCentersNearAddressAsync(address, radiusKm: 10.0);

            result.Should().BeEmpty();
        }

        #endregion
    }
}