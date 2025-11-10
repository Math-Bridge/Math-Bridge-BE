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
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System;

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

        private readonly List<Contract> _contracts;
        private readonly List<Child> _children;

        public CenterServiceTests()
        {
            _centerRepositoryMock = new Mock<ICenterRepository>();
            _tutorCenterRepositoryMock = new Mock<ITutorCenterRepository>();
            _userRepositoryMock = new Mock<IUserRepository>();
            _contextMock = new Mock<MathBridgeDbContext>();
            _googleMapsServiceMock = new Mock<IGoogleMapsService>();

            _contracts = new List<Contract>();
            _children = new List<Child>();

            _contextMock.Setup(c => c.Contracts).Returns(() => _contracts.AsQueryable().BuildMockDbSet().Object);
            _contextMock.Setup(c => c.Children).Returns(() => _children.AsQueryable().BuildMockDbSet().Object);

            _centerService = new CenterService(
                _centerRepositoryMock.Object,
                _tutorCenterRepositoryMock.Object,
                _userRepositoryMock.Object,
                _contextMock.Object,
                _googleMapsServiceMock.Object
            );
        }

        // Test: Ném lỗi nếu request là null
        [Fact]
        public async Task CreateCenterAsync_NullRequest_ThrowsArgumentNullException()
        {
            Func<Task> act = () => _centerService.CreateCenterAsync(null);
            await act.Should().ThrowAsync<ArgumentNullException>().WithParameterName("request");
        }

        // Test: Ném lỗi nếu thiếu tên trung tâm
        [Fact]
        public async Task CreateCenterAsync_MissingName_ThrowsArgumentException()
        {
            var request = new CreateCenterRequest { Name = " ", PlaceId = "valid-place-id" };
            Func<Task> act = () => _centerService.CreateCenterAsync(request);
            await act.Should().ThrowAsync<ArgumentException>().WithParameterName("Name");
        }

        // Test: Ném lỗi nếu thiếu PlaceId
        [Fact]
        public async Task CreateCenterAsync_MissingPlaceId_ThrowsArgumentException()
        {
            var request = new CreateCenterRequest { Name = "New Center", PlaceId = " " };
            Func<Task> act = () => _centerService.CreateCenterAsync(request);
            await act.Should().ThrowAsync<ArgumentException>().WithParameterName("PlaceId");
        }

        // Test: Ném lỗi nếu Google Maps API thất bại
        [Fact]
        public async Task CreateCenterAsync_GoogleMapsFails_ThrowsException()
        {
            var request = new CreateCenterRequest { Name = "New Center", PlaceId = "invalid-place-id" };
            _googleMapsServiceMock.Setup(s => s.GetPlaceDetailsAsync(request.PlaceId))
                .ReturnsAsync(new PlaceDetailsResponse { Success = false });

            Func<Task> act = () => _centerService.CreateCenterAsync(request);
            await act.Should().ThrowAsync<Exception>().WithMessage("Failed to fetch place details from Google Maps");
        }

        // Test: Tạo trung tâm thành công với request hợp lệ
        [Fact]
        public async Task CreateCenterAsync_ValidRequest_ReturnsCenterId()
        {
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

            var result = await _centerService.CreateCenterAsync(request);

            result.Should().NotBe(Guid.Empty);
            _centerRepositoryMock.Verify(repo => repo.AddAsync(It.IsAny<Center>()), Times.Once);
        }

        // Test: Ném lỗi nếu trung tâm bị trùng (tên + thành phố + quận)
        [Fact]
        public async Task CreateCenterAsync_DuplicateCenter_ThrowsException()
        {
            var request = new CreateCenterRequest { Name = "Existing Center", PlaceId = "valid-place-id" };
            var placeDetailsResponse = new PlaceDetailsResponse { Success = true, Place = new PlaceDetails { City = "City", District = "District" } };
            var existingCenters = new List<Center> { new Center { Name = "Existing Center", City = "City", District = "District" } };
            _googleMapsServiceMock.Setup(service => service.GetPlaceDetailsAsync(It.IsAny<string>())).ReturnsAsync(placeDetailsResponse);
            _centerRepositoryMock.Setup(repo => repo.GetAllAsync()).ReturnsAsync(existingCenters);

            Func<Task> act = () => _centerService.CreateCenterAsync(request);
            await act.Should().ThrowAsync<Exception>().WithMessage("*already exists*");
        }

        // Test: Ném lỗi nếu request cập nhật là null
        [Fact]
        public async Task UpdateCenterAsync_NullRequest_ThrowsArgumentNullException()
        {
            Func<Task> act = () => _centerService.UpdateCenterAsync(Guid.NewGuid(), null);
            await act.Should().ThrowAsync<ArgumentNullException>().WithParameterName("request");
        }

        // Test: Cập nhật trung tâm thành công với request hợp lệ
        [Fact]
        public async Task UpdateCenterAsync_ValidRequest_UpdatesCenter()
        {
            var id = Guid.NewGuid();
            var request = new UpdateCenterRequest { Name = "Updated Name", PlaceId = "new-place-id" };
            var existingCenter = new Center { CenterId = id, Name = "Old Name", GooglePlaceId = "old-place-id", City = "Old City", District = "Old District" };
            var placeDetailsResponse = new PlaceDetailsResponse { Success = true, Place = new PlaceDetails { City = "New City", District = "New District" } };
            _centerRepositoryMock.Setup(repo => repo.GetByIdAsync(id)).ReturnsAsync(existingCenter);
            _googleMapsServiceMock.Setup(service => service.GetPlaceDetailsAsync(It.IsAny<string>())).ReturnsAsync(placeDetailsResponse);
            _centerRepositoryMock.Setup(repo => repo.GetAllAsync()).ReturnsAsync(new List<Center>());
            _centerRepositoryMock.Setup(repo => repo.UpdateAsync(It.IsAny<Center>())).Returns(Task.CompletedTask);

            await _centerService.UpdateCenterAsync(id, request);

            _centerRepositoryMock.Verify(repo => repo.UpdateAsync(It.Is<Center>(c => c.Name == request.Name && c.City == "New City")), Times.Once);
        }

        // Test: Ném lỗi khi cập nhật tên trung tâm bị trùng
        [Fact]
        public async Task UpdateCenterAsync_DuplicateName_ThrowsException()
        {
            var id = Guid.NewGuid();
            var request = new UpdateCenterRequest { Name = "Existing Name" };
            var center = new Center { CenterId = id, Name = "Old Name", City = "City", District = "District" };
            var existingCenters = new List<Center>
            {
                center,
                new Center { CenterId = Guid.NewGuid(), Name = "Existing Name", City = "City", District = "District" }
            };

            _centerRepositoryMock.Setup(repo => repo.GetByIdAsync(id)).ReturnsAsync(center);
            _centerRepositoryMock.Setup(repo => repo.GetAllAsync()).ReturnsAsync(existingCenters);

            Func<Task> act = () => _centerService.UpdateCenterAsync(id, request);
            await act.Should().ThrowAsync<Exception>().WithMessage($"Another center with name 'Existing Name' at location City, District already exists");
        }

        // Test: Ném lỗi khi cập nhật địa điểm nhưng Google Maps thất bại
        [Fact]
        public async Task UpdateCenterAsync_GoogleMapsFails_ThrowsException()
        {
            var id = Guid.NewGuid();
            var request = new UpdateCenterRequest { PlaceId = "invalid-place-id" };
            var center = new Center { CenterId = id, GooglePlaceId = "old-place-id" };

            _centerRepositoryMock.Setup(repo => repo.GetByIdAsync(id)).ReturnsAsync(center);
            _googleMapsServiceMock.Setup(s => s.GetPlaceDetailsAsync(request.PlaceId))
                .ReturnsAsync(new PlaceDetailsResponse { Success = false });

            Func<Task> act = () => _centerService.UpdateCenterAsync(id, request);
            await act.Should().ThrowAsync<Exception>().WithMessage("Failed to fetch place details from Google Maps");
        }

        // Test: Không gọi UpdateAsync nếu không có gì thay đổi
        [Fact]
        public async Task UpdateCenterAsync_NoChanges_DoesNotCallUpdate()
        {
            var id = Guid.NewGuid();
            var request = new UpdateCenterRequest { Name = "Old Name", PlaceId = "old-place-id" };
            var center = new Center { CenterId = id, Name = "Old Name", GooglePlaceId = "old-place-id" };

            _centerRepositoryMock.Setup(repo => repo.GetByIdAsync(id)).ReturnsAsync(center);

            await _centerService.UpdateCenterAsync(id, request);

            _centerRepositoryMock.Verify(repo => repo.UpdateAsync(It.IsAny<Center>()), Times.Never);
        }

        // Test: Ném lỗi khi cập nhật trung tâm không tìm thấy
        [Fact]
        public async Task UpdateCenterAsync_CenterNotFound_ThrowsException()
        {
            var id = Guid.NewGuid();
            var request = new UpdateCenterRequest();
            _centerRepositoryMock.Setup(repo => repo.GetByIdAsync(id)).ReturnsAsync((Center)null);

            Func<Task> act = () => _centerService.UpdateCenterAsync(id, request);
            await act.Should().ThrowAsync<Exception>().WithMessage("Center not found");
        }

        // Test: Ném lỗi khi xóa trung tâm không tìm thấy
        [Fact]
        public async Task DeleteCenterAsync_CenterNotFound_ThrowsException()
        {
            _centerRepositoryMock.Setup(repo => repo.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((Center)null);
            Func<Task> act = () => _centerService.DeleteCenterAsync(Guid.NewGuid());
            await act.Should().ThrowAsync<Exception>().WithMessage("Center not found");
        }

        // Test: Xóa trung tâm thành công khi không có ràng buộc
        [Fact]
        public async Task DeleteCenterAsync_NoDependencies_DeletesCenter()
        {
            var id = Guid.NewGuid();
            var center = new Center { CenterId = id };
            _centerRepositoryMock.Setup(repo => repo.GetByIdAsync(id)).ReturnsAsync(center);
            _tutorCenterRepositoryMock.Setup(repo => repo.GetByCenterIdAsync(id)).ReturnsAsync(new List<TutorCenter>());

            _contracts.Clear();
            _children.Clear();

            _centerRepositoryMock.Setup(repo => repo.DeleteAsync(id)).Returns(Task.CompletedTask);

            await _centerService.DeleteCenterAsync(id);

            _centerRepositoryMock.Verify(repo => repo.DeleteAsync(id), Times.Once);
        }

        // Test: Ném lỗi khi xóa trung tâm còn hợp đồng (contract) active
        [Fact]
        public async Task DeleteCenterAsync_HasActiveContracts_ThrowsException()
        {
            var id = Guid.NewGuid();
            var center = new Center { CenterId = id };
            _centerRepositoryMock.Setup(repo => repo.GetByIdAsync(id)).ReturnsAsync(center);

            _contracts.Add(new Contract { CenterId = id, Status = "active" });

            var act = () => _centerService.DeleteCenterAsync(id);
            await act.Should().ThrowAsync<Exception>().WithMessage("Cannot delete center with active contracts");
        }

        // Test: Ném lỗi khi xóa trung tâm còn trẻ em (child) active
        [Fact]
        public async Task DeleteCenterAsync_HasAssignedChildren_ThrowsException()
        {
            var id = Guid.NewGuid();
            var center = new Center { CenterId = id };
            _centerRepositoryMock.Setup(repo => repo.GetByIdAsync(id)).ReturnsAsync(center);

            _contracts.Clear(); 
            _children.Add(new Child { CenterId = id, Status = "active" }); 

            var act = () => _centerService.DeleteCenterAsync(id);
            await act.Should().ThrowAsync<Exception>().WithMessage("Cannot delete center with assigned children");
        }

        // Test: Xóa thành công (sau khi xóa các liên kết tutor)
        [Fact]
        public async Task DeleteCenterAsync_WithTutorAssignments_RemovesAssignmentsAndDeletes()
        {
            var id = Guid.NewGuid();
            var center = new Center { CenterId = id };
            var tutorCenters = new List<TutorCenter>
            {
                new TutorCenter { TutorCenterId = Guid.NewGuid(), CenterId = id, TutorId = Guid.NewGuid() }
            };

            _centerRepositoryMock.Setup(repo => repo.GetByIdAsync(id)).ReturnsAsync(center);
            _contracts.Clear();
            _children.Clear();
            _tutorCenterRepositoryMock.Setup(repo => repo.GetByCenterIdAsync(id)).ReturnsAsync(tutorCenters);

            await _centerService.DeleteCenterAsync(id);

            _tutorCenterRepositoryMock.Verify(repo => repo.RemoveAsync(tutorCenters[0].TutorCenterId), Times.Once);
            _centerRepositoryMock.Verify(repo => repo.DeleteAsync(id), Times.Once);
        }

        // Test: Lấy trung tâm bằng ID thành công
        [Fact]
        public async Task GetCenterByIdAsync_ExistingCenter_ReturnsDto()
        {
            var id = Guid.NewGuid();
            var center = new Center { CenterId = id, Name = "Test Center" };
            _centerRepositoryMock.Setup(repo => repo.GetByIdAsync(id)).ReturnsAsync(center);

            var result = await _centerService.GetCenterByIdAsync(id);

            result.CenterId.Should().Be(id);
            result.Name.Should().Be("Test Center");
        }

        // Test: Ném lỗi khi lấy trung tâm bằng ID không tìm thấy
        [Fact]
        public async Task GetCenterByIdAsync_NonExisting_ThrowsException()
        {
            var id = Guid.NewGuid();
            _centerRepositoryMock.Setup(repo => repo.GetByIdAsync(id)).ReturnsAsync((Center)null);

            Func<Task> act = () => _centerService.GetCenterByIdAsync(id);
            await act.Should().ThrowAsync<Exception>().WithMessage("Center not found");
        }

        // Test: Lấy tất cả trung tâm (trả về danh sách)
        [Fact]
        public async Task GetAllCentersAsync_ReturnsList()
        {
            _centerRepositoryMock.Setup(repo => repo.GetAllAsync()).ReturnsAsync(new List<Center> { new Center(), new Center() });

            var result = await _centerService.GetAllCentersAsync();

            result.Should().HaveCount(2);
        }

        // Test: Lấy tất cả trung tâm (trả về danh sách rỗng)
        [Fact]
        public async Task GetAllCentersAsync_NoCenters_ReturnsEmptyList()
        {
            _centerRepositoryMock.Setup(repo => repo.GetAllAsync()).ReturnsAsync(new List<Center>());
            var result = await _centerService.GetAllCentersAsync();
            result.Should().BeEmpty();
        }

        // Test: Lấy danh sách trung tâm và các tutor trong đó
        [Fact]
        public async Task GetCentersWithTutorsAsync_ReturnsListWithTutors()
        {
            var centers = new List<Center> { new Center { CenterId = Guid.NewGuid() } };
            _centerRepositoryMock.Setup(repo => repo.GetAllAsync()).ReturnsAsync(centers);
            _userRepositoryMock.Setup(repo => repo.GetTutorsByCenterAsync(It.IsAny<Guid>())).ReturnsAsync(new List<User> { new User { TutorVerification = new TutorVerification() } });

            var result = await _centerService.GetCentersWithTutorsAsync();

            result.Should().HaveCount(1);
            result[0].Tutors.Should().HaveCount(1);
        }

        // Test: Tìm kiếm trung tâm theo bộ lọc (City)
        [Fact]
        public async Task SearchCentersAsync_WithFilters_ReturnsFilteredList()
        {
            var request = new CenterSearchRequest { City = "TestCity", Page = 1, PageSize = 10 };
            _centerRepositoryMock.Setup(repo => repo.GetAllAsync()).ReturnsAsync(new List<Center> { new Center { City = "TestCity" }, new Center { City = "Other" } });

            var result = await _centerService.SearchCentersAsync(request);

            result.Should().HaveCount(1);
            result[0].City.Should().Be("TestCity");
        }

        // Test: Tìm kiếm trung tâm với tất cả bộ lọc và phân trang
        [Fact]
        public async Task SearchCentersAsync_AllFiltersAndPagination_ReturnsCorrectList()
        {
            var centers = new List<Center>
            {
                new Center { Name = "Center A", City = "HCMC", District = "D1", Latitude = 10.0, Longitude = 106.0 },
                new Center { Name = "Center B", City = "HCMC", District = "D1", Latitude = 10.001, Longitude = 106.001 },
                new Center { Name = "Center C", City = "HCMC", District = "D2", Latitude = 10.1, Longitude = 106.1 },
                new Center { Name = "Center D", City = "Hanoi", District = "HK", Latitude = 21.0, Longitude = 105.0 }
            };
            _centerRepositoryMock.Setup(repo => repo.GetAllAsync()).ReturnsAsync(centers);

            var request = new CenterSearchRequest
            {
                City = "HCMC",
                District = "D1",
                Name = "Center",
                Latitude = 10.0,
                Longitude = 106.0,
                RadiusKm = 2,
                Page = 1,
                PageSize = 1
            };

            var result = await _centerService.SearchCentersAsync(request);

            result.Should().HaveCount(1);
            result[0].Name.Should().Be("Center A"); 
        }

        // Test: Đếm số lượng trung tâm theo tiêu chí
        [Fact]
        public async Task GetCentersCountByCriteriaAsync_ReturnsCount()
        {
            var request = new CenterSearchRequest { City = "TestCity" };
            _centerRepositoryMock.Setup(repo => repo.GetAllAsync()).ReturnsAsync(new List<Center> { new Center { City = "TestCity" } });

            var result = await _centerService.GetCentersCountByCriteriaAsync(request);

            result.Should().Be(1);
        }

        // Test: Lấy trung tâm theo thành phố
        [Fact]
        public async Task GetCentersByCityAsync_ValidCity_ReturnsList()
        {
            var city = "TestCity";
            _centerRepositoryMock.Setup(repo => repo.GetByCityAsync(city)).ReturnsAsync(new List<Center> { new Center() });

            var result = await _centerService.GetCentersByCityAsync(city);

            result.Should().HaveCount(1);
        }

        // Test: Lấy trung tâm theo thành phố (city là rỗng)
        [Fact]
        public async Task GetCentersByCityAsync_NullOrWhiteSpaceCity_ReturnsEmptyList()
        {
            var result = await _centerService.GetCentersByCityAsync(" ");
            result.Should().BeEmpty();
        }

        // Test: Lấy trung tâm gần vị trí (tọa độ)
        [Fact]
        public async Task GetCentersNearLocationAsync_ValidCoordinates_ReturnsList()
        {
            double lat = 10, lon = 20, radius = 10;
            _centerRepositoryMock.Setup(repo => repo.GetByCoordinates(lat, lon, radius)).ReturnsAsync(new List<Center> { new Center() });

            var result = await _centerService.GetCentersNearLocationAsync(lat, lon, radius);

            result.Should().HaveCount(1);
        }

        // Test: Ném lỗi khi lấy trung tâm gần vị trí với bán kính âm
        [Fact]
        public async Task GetCentersNearLocationAsync_NegativeRadius_ThrowsArgumentException()
        {
            Func<Task> act = () => _centerService.GetCentersNearLocationAsync(10, 10, -5);
            await act.Should().ThrowAsync<ArgumentException>().WithParameterName("radiusKm");
        }

        // Test: Gán tutor vào trung tâm thành công
        [Fact]
        public async Task AssignTutorToCenterAsync_ValidAssignment_AssignsTutor()
        {
            var centerId = Guid.NewGuid();
            var tutorId = Guid.NewGuid();
            var center = new Center();
            var tutor = new User { TutorVerification = new TutorVerification { VerificationStatus = "approved" } };
            _centerRepositoryMock.Setup(repo => repo.GetByIdAsync(centerId)).ReturnsAsync(center);
            _userRepositoryMock.Setup(repo => repo.GetTutorWithVerificationAsync(tutorId)).ReturnsAsync(tutor);
            _tutorCenterRepositoryMock.Setup(repo => repo.TutorIsAssignedToCenterAsync(tutorId, centerId)).ReturnsAsync(false);
            _tutorCenterRepositoryMock.Setup(repo => repo.AddAsync(It.IsAny<TutorCenter>())).Returns(Task.CompletedTask);
            _centerRepositoryMock.Setup(repo => repo.UpdateTutorCountAsync(centerId, 1)).Returns(Task.CompletedTask);

            await _centerService.AssignTutorToCenterAsync(centerId, tutorId);

            _tutorCenterRepositoryMock.Verify(repo => repo.AddAsync(It.IsAny<TutorCenter>()), Times.Once);
        }

        // Test: Ném lỗi khi gán tutor vào trung tâm (không tìm thấy trung tâm)
        [Fact]
        public async Task AssignTutorToCenterAsync_CenterNotFound_ThrowsException()
        {
            _centerRepositoryMock.Setup(repo => repo.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((Center)null);
            Func<Task> act = () => _centerService.AssignTutorToCenterAsync(Guid.NewGuid(), Guid.NewGuid());
            await act.Should().ThrowAsync<Exception>().WithMessage("Center not found");
        }

        // Test: Ném lỗi khi gán tutor vào trung tâm (không tìm thấy tutor)
        [Fact]
        public async Task AssignTutorToCenterAsync_TutorNotFound_ThrowsException()
        {
            _centerRepositoryMock.Setup(repo => repo.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync(new Center());
            _userRepositoryMock.Setup(repo => repo.GetTutorWithVerificationAsync(It.IsAny<Guid>())).ReturnsAsync((User)null);

            Func<Task> act = () => _centerService.AssignTutorToCenterAsync(Guid.NewGuid(), Guid.NewGuid());
            await act.Should().ThrowAsync<Exception>().WithMessage("Tutor not found");
        }

        // Test: Ném lỗi khi gán tutor đã được gán
        [Fact]
        public async Task AssignTutorToCenterAsync_TutorAlreadyAssigned_ThrowsException()
        {
            var centerId = Guid.NewGuid();
            var tutorId = Guid.NewGuid();
            _centerRepositoryMock.Setup(repo => repo.GetByIdAsync(centerId)).ReturnsAsync(new Center());
            _userRepositoryMock.Setup(repo => repo.GetTutorWithVerificationAsync(tutorId))
                .ReturnsAsync(new User { TutorVerification = new TutorVerification { VerificationStatus = "approved" } });
            _tutorCenterRepositoryMock.Setup(repo => repo.TutorIsAssignedToCenterAsync(tutorId, centerId)).ReturnsAsync(true);

            Func<Task> act = () => _centerService.AssignTutorToCenterAsync(centerId, tutorId);
            await act.Should().ThrowAsync<Exception>().WithMessage("Tutor is already assigned to this center");
        }

        // Test: Ném lỗi khi gán tutor chưa được duyệt (unverified)
        [Fact]
        public async Task AssignTutorToCenterAsync_UnverifiedTutor_ThrowsException()
        {
            var centerId = Guid.NewGuid();
            var tutorId = Guid.NewGuid();
            var center = new Center();
            var tutor = new User { TutorVerification = new TutorVerification { VerificationStatus = "pending" } };
            _centerRepositoryMock.Setup(repo => repo.GetByIdAsync(centerId)).ReturnsAsync(center);
            _userRepositoryMock.Setup(repo => repo.GetTutorWithVerificationAsync(tutorId)).ReturnsAsync(tutor);

            Func<Task> act = () => _centerService.AssignTutorToCenterAsync(centerId, tutorId);
            await act.Should().ThrowAsync<Exception>().WithMessage("Tutor must be verified*");
        }

        // Test: Xóa tutor khỏi trung tâm thành công
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

        // Test: Ném lỗi khi xóa tutor (tutor không được gán vào trung tâm này)
        [Fact]
        public async Task RemoveTutorFromCenterAsync_TutorNotAssigned_ThrowsException()
        {
            var centerId = Guid.NewGuid();
            var tutorId = Guid.NewGuid();
            _centerRepositoryMock.Setup(repo => repo.GetByIdAsync(centerId)).ReturnsAsync(new Center());
            _userRepositoryMock.Setup(repo => repo.GetTutorWithVerificationAsync(tutorId)).ReturnsAsync(new User());
            _tutorCenterRepositoryMock.Setup(repo => repo.TutorIsAssignedToCenterAsync(tutorId, centerId)).ReturnsAsync(false);

            Func<Task> act = () => _centerService.RemoveTutorFromCenterAsync(centerId, tutorId);
            await act.Should().ThrowAsync<Exception>().WithMessage("Tutor is not assigned to this center");
        }

        // Test: Lấy danh sách tutor theo ID trung tâm
        [Fact]
        public async Task GetTutorsByCenterIdAsync_ValidCenter_ReturnsTutors()
        {
            var centerId = Guid.NewGuid();
            var center = new Center();
            var tutors = new List<User> { new User { TutorVerification = new TutorVerification() } };
            _centerRepositoryMock.Setup(repo => repo.GetByIdAsync(centerId)).ReturnsAsync(center);
            _userRepositoryMock.Setup(repo => repo.GetTutorsByCenterAsync(centerId)).ReturnsAsync(tutors);

            var result = await _centerService.GetTutorsByCenterIdAsync(centerId);

            result.Should().HaveCount(1);
        }

        // Test: Ném lỗi khi lấy danh sách tutor (không tìm thấy trung tâm)
        [Fact]
        public async Task GetTutorsByCenterIdAsync_CenterNotFound_ThrowsException()
        {
            _centerRepositoryMock.Setup(repo => repo.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((Center)null);
            Func<Task> act = () => _centerService.GetTutorsByCenterIdAsync(Guid.NewGuid());
            await act.Should().ThrowAsync<Exception>().WithMessage("Center not found");
        }
    }
}