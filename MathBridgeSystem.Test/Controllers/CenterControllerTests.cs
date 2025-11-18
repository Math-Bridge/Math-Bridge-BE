using MathBridgeSystem.Api.Controllers;
using MathBridgeSystem.Application.DTOs;
using MathBridgeSystem.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using Assert = Xunit.Assert;

namespace MathBridgeSystem.Tests.Controllers
{
    public class CenterControllerTests
    {
        private readonly Mock<ICenterService> _mockCenterService;
        private readonly Mock<ILocationService> _mockLocationService;
        private readonly CenterController _controller;

        public CenterControllerTests()
        {
            _mockCenterService = new Mock<ICenterService>();
            _mockLocationService = new Mock<ILocationService>();
            _controller = new CenterController(_mockCenterService.Object, _mockLocationService.Object);
        }

        #region Constructor Tests
        [Fact]
        public void Constructor_NullCenterService_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => new CenterController(null!, _mockLocationService.Object));
        }

        [Fact]
        public void Constructor_NullLocationService_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => new CenterController(_mockCenterService.Object, null!));
        }
        #endregion

        #region CreateCenter Tests
        [Fact]
        public async Task CreateCenter_ValidRequest_ReturnsCreatedResult()
        {
            var request = new CreateCenterRequest { Name = "Test Center" };
            var centerId = Guid.NewGuid();
            _mockCenterService.Setup(s => s.CreateCenterAsync(request)).ReturnsAsync(centerId);
            var result = await _controller.CreateCenter(request);
            var createdResult = Assert.IsType<CreatedAtActionResult>(result);
            Assert.Equal(nameof(_controller.GetCenterById), createdResult.ActionName);
            _mockCenterService.Verify(s => s.CreateCenterAsync(request), Times.Once);
        }

        [Fact]
        public async Task CreateCenter_RequiredFieldMissing_ReturnsBadRequest()
        {
            var request = new CreateCenterRequest();
            _mockCenterService.Setup(s => s.CreateCenterAsync(request)).ThrowsAsync(new ArgumentException("Name is required"));
            var result = await _controller.CreateCenter(request);
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.NotNull(badRequestResult.Value);
        }

        [Fact]
        public async Task CreateCenter_DuplicateName_ReturnsConflict()
        {
            var request = new CreateCenterRequest { Name = "Existing Center" };
            _mockCenterService.Setup(s => s.CreateCenterAsync(request)).ThrowsAsync(new Exception("Center with this name already exists"));
            var result = await _controller.CreateCenter(request);
            var conflictResult = Assert.IsType<ConflictObjectResult>(result);
            Assert.NotNull(conflictResult.Value);
        }

        [Fact]
        public async Task CreateCenter_InvalidGoogleMaps_ReturnsBadRequest()
        {
            var request = new CreateCenterRequest { Name = "Test Center" };
            _mockCenterService.Setup(s => s.CreateCenterAsync(request)).ThrowsAsync(new Exception("Google Maps error"));
            var result = await _controller.CreateCenter(request);
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.NotNull(badRequestResult.Value);
        }

        [Fact]
        public async Task CreateCenter_InvalidModelState_ReturnsBadRequest()
        {
            var request = new CreateCenterRequest { Name = "Test Center" };
            _controller.ModelState.AddModelError("Name", "Name is required");
            var result = await _controller.CreateCenter(request);
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.IsType<SerializableError>(badRequestResult.Value);
            _mockCenterService.Verify(s => s.CreateCenterAsync(It.IsAny<CreateCenterRequest>()), Times.Never);
        }
        #endregion

        #region UpdateCenter Tests
        [Fact]
        public async Task UpdateCenter_ValidRequest_ReturnsOk()
        {
            var centerId = Guid.NewGuid();
            var request = new UpdateCenterRequest { Name = "Updated Center" };
            _mockCenterService.Setup(s => s.UpdateCenterAsync(centerId, request)).Returns(Task.CompletedTask);
            var result = await _controller.UpdateCenter(centerId, request);
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.NotNull(okResult.Value);
            _mockCenterService.Verify(s => s.UpdateCenterAsync(centerId, request), Times.Once);
        }

        [Fact]
        public async Task UpdateCenter_CenterNotFound_ReturnsNotFound()
        {
            var centerId = Guid.NewGuid();
            var request = new UpdateCenterRequest { Name = "Updated Center" };
            _mockCenterService.Setup(s => s.UpdateCenterAsync(centerId, request)).ThrowsAsync(new Exception("Center not found"));
            var result = await _controller.UpdateCenter(centerId, request);
            Assert.IsType<NotFoundObjectResult>(result);
        }

        [Fact]
        public async Task UpdateCenter_DuplicateName_ReturnsConflict()
        {
            var centerId = Guid.NewGuid();
            var request = new UpdateCenterRequest { Name = "Existing Center" };
            _mockCenterService.Setup(s => s.UpdateCenterAsync(centerId, request)).ThrowsAsync(new Exception("Center with this name already exists"));
            var result = await _controller.UpdateCenter(centerId, request);
            Assert.IsType<ConflictObjectResult>(result);
        }

        [Fact]
        public async Task UpdateCenter_GenericException_ReturnsInternalServerError()
        {
            var centerId = Guid.NewGuid();
            var request = new UpdateCenterRequest { Name = "Test Center" };
            _mockCenterService.Setup(s => s.UpdateCenterAsync(centerId, request)).ThrowsAsync(new Exception("Unexpected error"));
            var result = await _controller.UpdateCenter(centerId, request);
            var statusCodeResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(500, statusCodeResult.StatusCode);
        }

        [Fact]
        public async Task UpdateCenter_InvalidModelState_ReturnsBadRequest()
        {
            var centerId = Guid.NewGuid();
            var request = new UpdateCenterRequest { Name = "Updated Center" };
            _controller.ModelState.AddModelError("Name", "Name is invalid");
            var result = await _controller.UpdateCenter(centerId, request);
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.IsType<SerializableError>(badRequestResult.Value);
            _mockCenterService.Verify(s => s.UpdateCenterAsync(It.IsAny<Guid>(), It.IsAny<UpdateCenterRequest>()), Times.Never);
        }
        #endregion

        #region DeleteCenter Tests
        [Fact]
        public async Task DeleteCenter_ValidId_ReturnsNoContent()
        {
            var centerId = Guid.NewGuid();
            _mockCenterService.Setup(s => s.DeleteCenterAsync(centerId)).Returns(Task.CompletedTask);
            var result = await _controller.DeleteCenter(centerId);
            Assert.IsType<NoContentResult>(result);
            _mockCenterService.Verify(s => s.DeleteCenterAsync(centerId), Times.Once);
        }

        [Fact]
        public async Task DeleteCenter_NotFound_ReturnsNotFound()
        {
            var centerId = Guid.NewGuid();
            _mockCenterService.Setup(s => s.DeleteCenterAsync(centerId)).ThrowsAsync(new Exception("Center not found."));
            var result = await _controller.DeleteCenter(centerId);
            Assert.IsType<NotFoundObjectResult>(result);
        }

        [Fact]
        public async Task DeleteCenter_CannotDelete_ReturnsBadRequest()
        {
            var centerId = Guid.NewGuid();
            _mockCenterService.Setup(s => s.DeleteCenterAsync(centerId)).ThrowsAsync(new Exception("Cannot delete"));
            var result = await _controller.DeleteCenter(centerId);
            Assert.IsType<BadRequestObjectResult>(result);
        }
        #endregion

        #region GetCenterById Tests
        [Fact]
        public async Task GetCenterById_ValidId_ReturnsOk()
        {
            var centerId = Guid.NewGuid();
            var centerDto = new CenterDto { CenterId = centerId, Name = "Test Center" };
            _mockCenterService.Setup(s => s.GetCenterByIdAsync(centerId)).ReturnsAsync(centerDto);
            var result = await _controller.GetCenterById(centerId);
            var okResult = Assert.IsType<OkObjectResult>(result);
            var returnedCenter = Assert.IsType<CenterDto>(okResult.Value);
            Assert.Equal(centerId, returnedCenter.CenterId);
        }

        [Fact]
        public async Task GetCenterById_NotFound_ReturnsNotFound()
        {
            var centerId = Guid.NewGuid();
            _mockCenterService.Setup(s => s.GetCenterByIdAsync(centerId)).ThrowsAsync(new Exception("not found"));
            var result = await _controller.GetCenterById(centerId);
            Assert.IsType<NotFoundObjectResult>(result);
        }

        [Fact]
        public async Task GetCenterById_GenericException_ReturnsInternalServerError()
        {
            var centerId = Guid.NewGuid();
            _mockCenterService.Setup(s => s.GetCenterByIdAsync(centerId)).ThrowsAsync(new Exception("DB error"));
            var result = await _controller.GetCenterById(centerId);
            var statusCodeResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(500, statusCodeResult.StatusCode);
        }
        #endregion

        #region GetAllCenters Tests
        [Fact]
        public async Task GetAllCenters_ReturnsOkWithPaginatedData()
        {
            var centers = new List<CenterDto> { new CenterDto() };
            _mockCenterService.Setup(s => s.GetAllCentersAsync()).ReturnsAsync(centers);
            var result = await _controller.GetAllCenters(1, 20);
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.NotNull(okResult.Value);
        }

        [Fact]
        public async Task GetAllCenters_MultiplePages_ReturnsCorrectPagination()
        {
            var centers = new List<CenterDto>
            {
                new CenterDto { CenterId = Guid.NewGuid(), Name = "Center1", City = "CityA" },
                new CenterDto { CenterId = Guid.NewGuid(), Name = "Center2", City = "CityA" },
                new CenterDto { CenterId = Guid.NewGuid(), Name = "Center3", City = "CityB" },
                new CenterDto { CenterId = Guid.NewGuid(), Name = "Center4", City = "CityB" },
                new CenterDto { CenterId = Guid.NewGuid(), Name = "Center5", City = "CityB" }
            };
            _mockCenterService.Setup(s => s.GetAllCentersAsync()).ReturnsAsync(centers);
            var result = await _controller.GetAllCenters(2, 2);
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.NotNull(okResult.Value);
        }

        [Fact]
        public async Task GetAllCenters_GenericException_ReturnsInternalServerError()
        {
            _mockCenterService.Setup(s => s.GetAllCentersAsync()).ThrowsAsync(new Exception("DB error"));
            var result = await _controller.GetAllCenters(1, 20);
            var statusCodeResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(500, statusCodeResult.StatusCode);
        }
        #endregion

        #region GetCentersWithTutors Tests
        [Fact]
        public async Task GetCentersWithTutors_ReturnsOkWithPaginatedData()
        {
            var centers = new List<CenterWithTutorsDto> { new CenterWithTutorsDto() };
            _mockCenterService.Setup(s => s.GetCentersWithTutorsAsync()).ReturnsAsync(centers);
            var result = await _controller.GetCentersWithTutors(1, 10);
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.NotNull(okResult.Value);
        }

        [Fact]
        public async Task GetCentersWithTutors_MultiplePages_ReturnsCorrectPagination()
        {
            var centers = new List<CenterWithTutorsDto>
            {
                new CenterWithTutorsDto { CenterId = Guid.NewGuid(), Name = "Center1", City = "CityA", TutorCount = 1 },
                new CenterWithTutorsDto { CenterId = Guid.NewGuid(), Name = "Center2", City = "CityA", TutorCount = 2 },
                new CenterWithTutorsDto { CenterId = Guid.NewGuid(), Name = "Center3", City = "CityB", TutorCount = 0 }
            };
            _mockCenterService.Setup(s => s.GetCentersWithTutorsAsync()).ReturnsAsync(centers);
            var result = await _controller.GetCentersWithTutors(1, 2);
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.NotNull(okResult.Value);
        }

        [Fact]
        public async Task GetCentersWithTutors_GenericException_ReturnsInternalServerError()
        {
            _mockCenterService.Setup(s => s.GetCentersWithTutorsAsync()).ThrowsAsync(new Exception("DB error"));
            var result = await _controller.GetCentersWithTutors(1, 10);
            var statusCodeResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(500, statusCodeResult.StatusCode);
        }
        #endregion

        #region SearchCenters Tests
        [Fact]
        public async Task SearchCenters_ValidRequest_ReturnsOk()
        {
            var centers = new List<CenterDto> { new CenterDto() };
            _mockCenterService.Setup(s => s.SearchCentersAsync(It.IsAny<CenterSearchRequest>())).ReturnsAsync(centers);
            _mockCenterService.Setup(s => s.GetCentersCountByCriteriaAsync(It.IsAny<CenterSearchRequest>())).ReturnsAsync(1);
            var result = await _controller.SearchCenters(city: "Test City");
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.NotNull(okResult.Value);
        }

        [Fact]
        public async Task SearchCenters_InvalidPage_ClampsToMinimum()
        {
            var centers = new List<CenterDto> { new CenterDto() };
            _mockCenterService.Setup(s => s.SearchCentersAsync(It.IsAny<CenterSearchRequest>())).ReturnsAsync(centers);
            _mockCenterService.Setup(s => s.GetCentersCountByCriteriaAsync(It.IsAny<CenterSearchRequest>())).ReturnsAsync(1);
            var result = await _controller.SearchCenters(city: "Test City", page: 0);
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.NotNull(okResult.Value);
            // Verify that page was clamped to 1
            var responseObj = JObject.FromObject(okResult.Value);
            Assert.Equal(1, responseObj["pagination"]["currentPage"].Value<int>());
        }

        [Fact]
        public async Task SearchCenters_InvalidPageSize_ClampsToMaximum()
        {
            var centers = new List<CenterDto> { new CenterDto() };
            _mockCenterService.Setup(s => s.SearchCentersAsync(It.IsAny<CenterSearchRequest>())).ReturnsAsync(centers);
            _mockCenterService.Setup(s => s.GetCentersCountByCriteriaAsync(It.IsAny<CenterSearchRequest>())).ReturnsAsync(1);
            var result = await _controller.SearchCenters(city: "Test City", pageSize: 100);
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.NotNull(okResult.Value);
            // Verify that pageSize was clamped to 50
            var responseObj = JObject.FromObject(okResult.Value);
            Assert.Equal(50, responseObj["pagination"]["pageSize"].Value<int>());
        }

        [Fact]
        public async Task SearchCenters_GenericException_ReturnsInternalServerError()
        {
            _mockCenterService.Setup(s => s.SearchCentersAsync(It.IsAny<CenterSearchRequest>())).ThrowsAsync(new Exception("DB Error"));
            var result = await _controller.SearchCenters(city: "Test City");
            var statusCodeResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(500, statusCodeResult.StatusCode);
        }
        #endregion

        #region GetCentersByCity Tests
        [Fact]
        public async Task GetCentersByCity_ValidCity_ReturnsOk()
        {
            var city = "Test City";
            var centers = new List<CenterDto> { new CenterDto { City = city } };
            _mockCenterService.Setup(s => s.GetCentersByCityAsync(city)).ReturnsAsync(centers);
            var result = await _controller.GetCentersByCity(city);
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.NotNull(okResult.Value);
        }

        [Fact]
        public async Task GetCentersByCity_EmptyCity_ReturnsBadRequest()
        {
            var result = await _controller.GetCentersByCity(" ");
            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task GetCentersByCity_MultipleCenters_ReturnsPaginatedData()
        {
            var city = "Test City";
            var centers = new List<CenterDto>
            {
                new CenterDto { CenterId = Guid.NewGuid(), Name = "Center A", City = city },
                new CenterDto { CenterId = Guid.NewGuid(), Name = "Center B", City = city },
                new CenterDto { CenterId = Guid.NewGuid(), Name = "Center C", City = city }
            };
            _mockCenterService.Setup(s => s.GetCentersByCityAsync(city)).ReturnsAsync(centers);
            var result = await _controller.GetCentersByCity(city, 1, 2);
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.NotNull(okResult.Value);
            var responseObj = JObject.FromObject(okResult.Value);
            Assert.Equal(city, responseObj["city"].Value<string>());
            var pagination = responseObj["pagination"];
            Assert.Equal(1, pagination["currentPage"].Value<int>());
            Assert.Equal(2, pagination["pageSize"].Value<int>());
            Assert.Equal(3, pagination["totalCount"].Value<int>());
            Assert.Equal(2, pagination["totalPages"].Value<int>());
            Assert.True(pagination["hasNext"].Value<bool>());
            Assert.False(pagination["hasPrevious"].Value<bool>());
            Assert.Equal(2, ((JArray)responseObj["data"]).Count);
        }

        [Fact]
        public async Task GetCentersByCity_GenericException_ReturnsInternalServerError()
        {
            var city = "Test City";
            _mockCenterService.Setup(s => s.GetCentersByCityAsync(city)).ThrowsAsync(new Exception("DB error"));
            var result = await _controller.GetCentersByCity(city);
            var statusCodeResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(500, statusCodeResult.StatusCode);
        }
        #endregion

        #region AssignTutorToCenter Tests
        [Fact]
        public async Task AssignTutorToCenter_ValidRequest_ReturnsOk()
        {
            var centerId = Guid.NewGuid();
            var tutorId = Guid.NewGuid();
            var request = new AssignTutorRequest { TutorId = tutorId };
            _mockCenterService.Setup(s => s.AssignTutorToCenterAsync(centerId, tutorId)).Returns(Task.CompletedTask);
            var result = await _controller.AssignTutorToCenter(centerId, request);
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.NotNull(okResult.Value);
            _mockCenterService.Verify(s => s.AssignTutorToCenterAsync(centerId, tutorId), Times.Once);
        }

        [Fact]
        public async Task AssignTutorToCenter_InvalidModelState_ReturnsBadRequest()
        {
            var centerId = Guid.NewGuid();
            var request = new AssignTutorRequest { TutorId = Guid.Empty };
            _controller.ModelState.AddModelError("TutorId", "TutorId is required");
            var result = await _controller.AssignTutorToCenter(centerId, request);
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.IsType<SerializableError>(badRequestResult.Value);
            _mockCenterService.Verify(s => s.AssignTutorToCenterAsync(It.IsAny<Guid>(), It.IsAny<Guid>()), Times.Never);
        }

        [Fact]
        public async Task AssignTutorToCenter_NotFound_ReturnsNotFound()
        {
            var centerId = Guid.NewGuid();
            var tutorId = Guid.NewGuid();
            var request = new AssignTutorRequest { TutorId = tutorId };
            _mockCenterService.Setup(s => s.AssignTutorToCenterAsync(centerId, tutorId)).ThrowsAsync(new Exception("not found"));
            var result = await _controller.AssignTutorToCenter(centerId, request);
            Assert.IsType<NotFoundObjectResult>(result);
        }

        [Fact]
        public async Task AssignTutorToCenter_Conflict_ReturnsConflict()
        {
            var centerId = Guid.NewGuid();
            var tutorId = Guid.NewGuid();
            var request = new AssignTutorRequest { TutorId = tutorId };
            _mockCenterService.Setup(s => s.AssignTutorToCenterAsync(centerId, tutorId)).ThrowsAsync(new Exception("already assigned"));
            var result = await _controller.AssignTutorToCenter(centerId, request);
            Assert.IsType<ConflictObjectResult>(result);
        }

        [Fact]
        public async Task AssignTutorToCenter_TutorNotVerified_ReturnsBadRequest()
        {
            var centerId = Guid.NewGuid();
            var tutorId = Guid.NewGuid();
            var request = new AssignTutorRequest { TutorId = tutorId };
            _mockCenterService.Setup(s => s.AssignTutorToCenterAsync(centerId, tutorId)).ThrowsAsync(new Exception("verified"));
            var result = await _controller.AssignTutorToCenter(centerId, request);
            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task AssignTutorToCenter_GenericException_ReturnsBadRequest()
        {
            var centerId = Guid.NewGuid();
            var tutorId = Guid.NewGuid();
            var request = new AssignTutorRequest { TutorId = tutorId };
            _mockCenterService.Setup(s => s.AssignTutorToCenterAsync(centerId, tutorId)).ThrowsAsync(new Exception("Unknown error"));
            var result = await _controller.AssignTutorToCenter(centerId, request);
            var badRequestResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(400, badRequestResult.StatusCode);
            Assert.NotNull(badRequestResult.Value);
        }
        #endregion

        #region RemoveTutorFromCenter Tests
        [Fact]
        public async Task RemoveTutorFromCenter_ValidRequest_ReturnsOk()
        {
            var centerId = Guid.NewGuid();
            var tutorId = Guid.NewGuid();
            _mockCenterService.Setup(s => s.RemoveTutorFromCenterAsync(centerId, tutorId)).Returns(Task.CompletedTask);
            var result = await _controller.RemoveTutorFromCenter(centerId, tutorId);
            Assert.IsType<OkObjectResult>(result);
            _mockCenterService.Verify(s => s.RemoveTutorFromCenterAsync(centerId, tutorId), Times.Once);
        }

        [Fact]
        public async Task RemoveTutorFromCenter_NotFound_ReturnsNotFound()
        {
            var centerId = Guid.NewGuid();
            var tutorId = Guid.NewGuid();
            _mockCenterService.Setup(s => s.RemoveTutorFromCenterAsync(centerId, tutorId)).ThrowsAsync(new Exception("not found"));
            var result = await _controller.RemoveTutorFromCenter(centerId, tutorId);
            Assert.IsType<NotFoundObjectResult>(result);
        }

        [Fact]
        public async Task RemoveTutorFromCenter_NotAssigned_ReturnsBadRequest()
        {
            var centerId = Guid.NewGuid();
            var tutorId = Guid.NewGuid();
            _mockCenterService.Setup(s => s.RemoveTutorFromCenterAsync(centerId, tutorId)).ThrowsAsync(new Exception("not assigned"));
            var result = await _controller.RemoveTutorFromCenter(centerId, tutorId);
            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task RemoveTutorFromCenter_GenericException_ReturnsInternalServerError()
        {
            var centerId = Guid.NewGuid();
            var tutorId = Guid.NewGuid();
            _mockCenterService.Setup(s => s.RemoveTutorFromCenterAsync(centerId, tutorId)).ThrowsAsync(new Exception("Unknown error"));
            var result = await _controller.RemoveTutorFromCenter(centerId, tutorId);
            var statusCodeResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(500, statusCodeResult.StatusCode);
        }
        #endregion

        #region GetTutorsByCenterId Tests
        [Fact]
        public async Task GetTutorsByCenterId_ValidId_ReturnsOk()
        {
            var centerId = Guid.NewGuid();
            var tutors = new List<TutorInCenterDto> { new TutorInCenterDto() };
            _mockCenterService.Setup(s => s.GetTutorsByCenterIdAsync(centerId)).ReturnsAsync(tutors);
            var result = await _controller.GetTutorsByCenterId(centerId);
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.IsType<List<TutorInCenterDto>>(okResult.Value);
        }

        [Fact]
        public async Task GetTutorsByCenterId_NotFound_ReturnsNotFound()
        {
            var centerId = Guid.NewGuid();
            _mockCenterService.Setup(s => s.GetTutorsByCenterIdAsync(centerId)).ThrowsAsync(new Exception("not found"));
            var result = await _controller.GetTutorsByCenterId(centerId);
            Assert.IsType<NotFoundObjectResult>(result);
        }

        [Fact]
        public async Task GetTutorsByCenterId_GenericException_ReturnsInternalServerError()
        {
            var centerId = Guid.NewGuid();
            _mockCenterService.Setup(s => s.GetTutorsByCenterIdAsync(centerId)).ThrowsAsync(new Exception("DB error"));
            var result = await _controller.GetTutorsByCenterId(centerId);
            var statusCodeResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(500, statusCodeResult.StatusCode);
        }
        #endregion

        #region GetCenterStatistics Tests
        [Fact]
        public async Task GetCenterStatistics_ReturnsOk()
        {
            _mockCenterService.Setup(s => s.GetAllCentersAsync()).ReturnsAsync(new List<CenterDto>());
            _mockCenterService.Setup(s => s.GetCentersWithTutorsAsync()).ReturnsAsync(new List<CenterWithTutorsDto>());
            var result = await _controller.GetCenterStatistics();
            Assert.IsType<OkObjectResult>(result);
        }

        [Fact]
        public async Task GetCenterStatistics_GenericException_ReturnsInternalServerError()
        {
            _mockCenterService.Setup(s => s.GetAllCentersAsync()).ThrowsAsync(new Exception("DB error"));
            var result = await _controller.GetCenterStatistics();
            var statusCodeResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(500, statusCodeResult.StatusCode);
        }

        [Fact]
        public async Task GetCenterStatistics_WithData_ReturnsCorrectStatistics()
        {
            var centers = new List<CenterDto>
            {
                new CenterDto { CenterId = Guid.NewGuid(), Name = "Center1", City = "CityA", Latitude = 10.0, TutorCount = 2 },
                new CenterDto { CenterId = Guid.NewGuid(), Name = "Center2", City = "CityA", Latitude = 20.0, TutorCount = 1 },
                new CenterDto { CenterId = Guid.NewGuid(), Name = "Center3", City = "CityB", Latitude = 30.0, TutorCount = 0 }
            };
            var centersWithTutors = new List<CenterWithTutorsDto>
            {
                new CenterWithTutorsDto { CenterId = Guid.NewGuid(), Name = "Center1", City = "CityA", TutorCount = 2 },
                new CenterWithTutorsDto { CenterId = Guid.NewGuid(), Name = "Center2", City = "CityA", TutorCount = 1 },
                new CenterWithTutorsDto { CenterId = Guid.NewGuid(), Name = "Center3", City = "CityB", TutorCount = 0 }
            };
            _mockCenterService.Setup(s => s.GetAllCentersAsync()).ReturnsAsync(centers);
            _mockCenterService.Setup(s => s.GetCentersWithTutorsAsync()).ReturnsAsync(centersWithTutors);
            var result = await _controller.GetCenterStatistics();
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.NotNull(okResult.Value);
            var stats = JObject.FromObject(okResult.Value);
            Assert.Equal(3, stats["totalCenters"].Value<int>());
            Assert.Equal(2, stats["centersWithTutors"].Value<int>());
            Assert.Equal(3, stats["totalTutors"].Value<int>());
            Assert.Equal(1.0, stats["avgTutorsPerCenter"].Value<double>());
            Assert.Equal(2, stats["cities"].Value<int>());
            var mostActiveCity = stats["mostActiveCity"];
            Assert.Equal("CityA", mostActiveCity["City"].Value<string>());
            Assert.Equal(2, mostActiveCity["Count"].Value<int>());
            Assert.Equal(20.0, stats["avgDistanceFromCenter"].Value<double>());
        }
        #endregion

        #region GetCentersNearLocation Tests
        [Fact]
        public async Task GetCentersNearLocation_ReturnsRedirect()
        {
            var address = "Test Address";
            var radiusKm = 5.0;
            var page = 1;
            var pageSize = 10;
            var result = await _controller.GetCentersNearLocation(address, radiusKm, page, pageSize);
            var redirectResult = Assert.IsType<RedirectResult>(result);
            Assert.Contains("/api/location/nearby-centers", redirectResult.Url);
            Assert.Contains(Uri.EscapeDataString(address), redirectResult.Url);
            Assert.Contains(radiusKm.ToString(), redirectResult.Url);
        }
        #endregion
    }
}
