using FluentAssertions;
using MathBridgeSystem.Api.Controllers;
using MathBridgeSystem.Application.DTOs.TestResult;
using MathBridgeSystem.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace MathBridgeSystem.Test.Controllers
{
    public class TestResultControllerTests
    {
        private readonly Mock<ITestResultService> _serviceMock;
        private readonly TestResultController _controller;

        public TestResultControllerTests()
        {
            _serviceMock = new Mock<ITestResultService>();
            _controller = new TestResultController(_serviceMock.Object);
        }

        [Fact]
        public void Constructor_NullService_Throws()
        {
            Action act = () => new TestResultController(null!);
            act.Should().Throw<ArgumentNullException>().WithParameterName("testResultService");
        }

        [Fact]
        public async Task GetTestResultById_ReturnsOk()
        {
            var id = Guid.NewGuid();
            var dto = new TestResultDto { ResultId = id, Score = 90 };
            _serviceMock.Setup(s => s.GetTestResultByIdAsync(id)).ReturnsAsync(dto);

            var result = await _controller.GetTestResultById(id);

            var ok = result.Should().BeOfType<OkObjectResult>().Subject;
            ok.Value.Should().BeAssignableTo<TestResultDto>();
        }

        [Fact]
        public async Task GetTestResultById_NotFound()
        {
            var id = Guid.NewGuid();
            _serviceMock.Setup(s => s.GetTestResultByIdAsync(id)).ThrowsAsync(new KeyNotFoundException("not"));

            var result = await _controller.GetTestResultById(id);

            result.Should().BeOfType<NotFoundObjectResult>();
        }

        [Fact]
        public async Task GetTestResultById_ServerError()
        {
            var id = Guid.NewGuid();
            _serviceMock.Setup(s => s.GetTestResultByIdAsync(id)).ThrowsAsync(new Exception("boom"));

            var result = await _controller.GetTestResultById(id);

            var obj = result.Should().BeOfType<ObjectResult>().Subject;
            obj.StatusCode.Should().Be(500);
        }

        [Fact]
        public async Task GetTestResultsByContractId_ReturnsOk()
        {
            var contractId = Guid.NewGuid();
            var list = new List<TestResultDto> { new TestResultDto { ResultId = Guid.NewGuid() } };
            _serviceMock.Setup(s => s.GetTestResultsByContractIdAsync(contractId)).ReturnsAsync(list);

            var result = await _controller.GetTestResultsByContractId(contractId);

            var ok = result.Should().BeOfType<OkObjectResult>().Subject;
            ok.Value.Should().BeAssignableTo<IEnumerable<TestResultDto>>();
        }

        [Fact]
        public async Task GetTestResultsByContractId_ServerError()
        {
            var contractId = Guid.NewGuid();
            _serviceMock.Setup(s => s.GetTestResultsByContractIdAsync(contractId)).ThrowsAsync(new Exception("boom"));

            var result = await _controller.GetTestResultsByContractId(contractId);

            var obj = result.Should().BeOfType<ObjectResult>().Subject;
            obj.StatusCode.Should().Be(500);
        }

        [Fact]
        public async Task CreateTestResult_ModelInvalid_ReturnsBadRequest()
        {
            _controller.ModelState.AddModelError("StudentId", "Required");

            var result = await _controller.CreateTestResult(new CreateTestResultRequest());

            result.Should().BeOfType<BadRequestObjectResult>();
        }

        [Fact]
        public async Task CreateTestResult_ArgumentException_ReturnsBadRequest()
        {
            var req = new CreateTestResultRequest { ContractId = Guid.NewGuid() };
            _serviceMock.Setup(s => s.CreateTestResultAsync(req)).ThrowsAsync(new ArgumentException("bad"));

            var result = await _controller.CreateTestResult(req);

            result.Should().BeOfType<BadRequestObjectResult>();
        }

        [Fact]
        public async Task CreateTestResult_Success_ReturnsCreated()
        {
            var req = new CreateTestResultRequest { ContractId = Guid.NewGuid() };
            var id = Guid.NewGuid();
            _serviceMock.Setup(s => s.CreateTestResultAsync(req)).ReturnsAsync(id);

            var result = await _controller.CreateTestResult(req);

            var created = result.Should().BeOfType<CreatedAtActionResult>().Subject;
            created.Value.Should().NotBeNull();
        }

        [Fact]
        public async Task CreateTestResult_ServerError_Returns500()
        {
            var req = new CreateTestResultRequest { ContractId = Guid.NewGuid() };
            _serviceMock.Setup(s => s.CreateTestResultAsync(req)).ThrowsAsync(new Exception("boom"));

            var result = await _controller.CreateTestResult(req);

            var obj = result.Should().BeOfType<ObjectResult>().Subject;
            obj.StatusCode.Should().Be(500);
        }

        [Fact]
        public async Task UpdateTestResult_ModelInvalid_ReturnsBadRequest()
        {
            _controller.ModelState.AddModelError("Score", "Required");
            var id = Guid.NewGuid();

            var result = await _controller.UpdateTestResult(id, new UpdateTestResultRequest());

            result.Should().BeOfType<BadRequestObjectResult>();
        }

        [Fact]
        public async Task UpdateTestResult_NotFound_ReturnsNotFound()
        {
            var id = Guid.NewGuid();
            _serviceMock.Setup(s => s.UpdateTestResultAsync(id, It.IsAny<UpdateTestResultRequest>())).ThrowsAsync(new KeyNotFoundException("not"));

            var result = await _controller.UpdateTestResult(id, new UpdateTestResultRequest());

            result.Should().BeOfType<NotFoundObjectResult>();
        }

        [Fact]
        public async Task UpdateTestResult_ArgumentException_ReturnsBadRequest()
        {
            var id = Guid.NewGuid();
            _serviceMock.Setup(s => s.UpdateTestResultAsync(id, It.IsAny<UpdateTestResultRequest>())).ThrowsAsync(new ArgumentException("bad"));

            var result = await _controller.UpdateTestResult(id, new UpdateTestResultRequest());

            result.Should().BeOfType<BadRequestObjectResult>();
        }

        [Fact]
        public async Task UpdateTestResult_Success_ReturnsOk()
        {
            var id = Guid.NewGuid();
            _serviceMock.Setup(s => s.UpdateTestResultAsync(id, It.IsAny<UpdateTestResultRequest>())).Returns(Task.CompletedTask);

            var result = await _controller.UpdateTestResult(id, new UpdateTestResultRequest());

            var ok = result.Should().BeOfType<OkObjectResult>().Subject;
            ok.Value.Should().NotBeNull();
        }

        [Fact]
        public async Task UpdateTestResult_ServerError_Returns500()
        {
            var id = Guid.NewGuid();
            _serviceMock.Setup(s => s.UpdateTestResultAsync(id, It.IsAny<UpdateTestResultRequest>())).ThrowsAsync(new Exception("boom"));

            var result = await _controller.UpdateTestResult(id, new UpdateTestResultRequest());

            var obj = result.Should().BeOfType<ObjectResult>().Subject;
            obj.StatusCode.Should().Be(500);
        }

        [Fact]
        public async Task DeleteTestResult_Success_ReturnsOk()
        {
            var id = Guid.NewGuid();
            _serviceMock.Setup(s => s.DeleteTestResultAsync(id)).ReturnsAsync(true);

            var result = await _controller.DeleteTestResult(id);

            var ok = result.Should().BeOfType<OkObjectResult>().Subject;
            ok.Value.Should().NotBeNull();
        }

        [Fact]
        public async Task DeleteTestResult_NotFound_ReturnsNotFound()
        {
            var id = Guid.NewGuid();
            _serviceMock.Setup(s => s.DeleteTestResultAsync(id)).ReturnsAsync(false);

            var result = await _controller.DeleteTestResult(id);

            result.Should().BeOfType<NotFoundObjectResult>();
        }

        [Fact]
        public async Task DeleteTestResult_KeyNotFound_ReturnsNotFound()
        {
            var id = Guid.NewGuid();
            _serviceMock.Setup(s => s.DeleteTestResultAsync(id)).ThrowsAsync(new KeyNotFoundException("not"));

            var result = await _controller.DeleteTestResult(id);

            result.Should().BeOfType<NotFoundObjectResult>();
        }

        [Fact]
        public async Task DeleteTestResult_ServerError_Returns500()
        {
            var id = Guid.NewGuid();
            _serviceMock.Setup(s => s.DeleteTestResultAsync(id)).ThrowsAsync(new Exception("boom"));

            var result = await _controller.DeleteTestResult(id);

            var obj = result.Should().BeOfType<ObjectResult>().Subject;
            obj.StatusCode.Should().Be(500);
        }
    }
}
