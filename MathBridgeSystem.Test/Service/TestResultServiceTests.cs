using FluentAssertions;
using MathBridgeSystem.Application.DTOs.TestResult;
using MathBridgeSystem.Application.Services;
using MathBridgeSystem.Domain.Interfaces;
using Moq;
using Xunit;
using TestResultEntity = MathBridgeSystem.Domain.Entities.TestResult;

namespace MathBridgeSystem.Tests.Controllers
{
    public class TestResultServiceTests
    {
        private readonly Mock<ITestResultRepository> _testResultRepositoryMock;
        private readonly TestResultService _service;

        public TestResultServiceTests()
        {
            _testResultRepositoryMock = new Mock<ITestResultRepository>();
            _service = new TestResultService(_testResultRepositoryMock.Object);
        }

        [Fact]
        public async Task GetTestResultByIdAsync_ShouldReturnTestResult_WhenExists()
        {
            // Arrange
            var resultId = Guid.NewGuid();
            var testResult = new TestResultEntity
            {
                ResultId = resultId,
                TestType = "Diagnostic",
                Score = 85,
                Notes = "Good performance",
                ContractId = Guid.NewGuid()
            };

            _testResultRepositoryMock.Setup(r => r.GetByIdAsync(resultId))
                .ReturnsAsync(testResult);

            // Act
            var result = await _service.GetTestResultByIdAsync(resultId);

            // Assert
            result.Should().NotBeNull();
            result.ResultId.Should().Be(resultId);
            result.Score.Should().Be(85);
            result.TestType.Should().Be("Diagnostic");
        }

        [Fact]
        public async Task GetTestResultByIdAsync_ShouldThrowKeyNotFoundException_WhenNotExists()
        {
            // Arrange
            var resultId = Guid.NewGuid();
            _testResultRepositoryMock.Setup(r => r.GetByIdAsync(resultId))
                .ReturnsAsync((TestResultEntity)null!);

            // Act & Assert
            await Xunit.Assert.ThrowsAsync<KeyNotFoundException>(
                async () => await _service.GetTestResultByIdAsync(resultId)
            );
        }

        [Fact]
        public async Task GetTestResultsByContractIdAsync_ShouldReturnAllResults()
        {
            // Arrange
            var contractId = Guid.NewGuid();
            var testResults = new List<TestResultEntity>
            {
                new TestResultEntity { ResultId = Guid.NewGuid(), ContractId = contractId, Score = 80 },
                new TestResultEntity { ResultId = Guid.NewGuid(), ContractId = contractId, Score = 90 }
            };

            _testResultRepositoryMock.Setup(r => r.GetByContractIdAsync(contractId))
                .ReturnsAsync(testResults);

            // Act
            var result = await _service.GetTestResultsByContractIdAsync(contractId);

            // Assert
            result.Should().HaveCount(2);
        }

        [Fact]
        public async Task CreateTestResultAsync_ShouldCreateAndReturnId()
        {
            // Arrange
            var contractId = Guid.NewGuid();
            var request = new CreateTestResultRequest
            {
                TestType = "Final",
                Score = 95,
                Notes = "Excellent",
                ContractId = contractId
            };

            _testResultRepositoryMock.Setup(r => r.AddAsync(It.IsAny<TestResultEntity>()))
                .ReturnsAsync((TestResultEntity t) => t);

            // Act
            var resultId = await _service.CreateTestResultAsync(request);

            // Assert
            resultId.Should().NotBeEmpty();
            _testResultRepositoryMock.Verify(r => r.AddAsync(It.Is<TestResultEntity>(
                t => t.TestType == "Final" && t.Score == 95
            )), Times.Once);
        }

        [Fact]
        public async Task UpdateTestResultAsync_ShouldUpdateTestResult_WhenExists()
        {
            // Arrange
            var resultId = Guid.NewGuid();
            var testResult = new TestResultEntity
            {
                ResultId = resultId,
                TestType = "Diagnostic",
                Score = 80,
                Notes = "Good"
            };

            var request = new UpdateTestResultRequest
            {
                TestType = "Final",
                Score = 90,
                Notes = "Excellent"
            };

            _testResultRepositoryMock.Setup(r => r.GetByIdAsync(resultId))
                .ReturnsAsync(testResult);

            // Act
            await _service.UpdateTestResultAsync(resultId, request);

            // Assert
            testResult.TestType.Should().Be("Final");
            testResult.Score.Should().Be(90);
            testResult.Notes.Should().Be("Excellent");
            _testResultRepositoryMock.Verify(r => r.UpdateAsync(testResult), Times.Once);
        }

        [Fact]
        public async Task DeleteTestResultAsync_ShouldDeleteTestResult_WhenExists()
        {
            // Arrange
            var resultId = Guid.NewGuid();
            var testResult = new TestResultEntity { ResultId = resultId };

            _testResultRepositoryMock.Setup(r => r.GetByIdAsync(resultId))
                .ReturnsAsync(testResult);
            _testResultRepositoryMock.Setup(r => r.DeleteAsync(resultId))
                .ReturnsAsync(true);

            // Act
            var result = await _service.DeleteTestResultAsync(resultId);

            // Assert
            result.Should().BeTrue();
            _testResultRepositoryMock.Verify(r => r.DeleteAsync(resultId), Times.Once);
        }

        [Fact]
        public void Constructor_ShouldThrowArgumentNullException_WhenRepositoryIsNull()
        {
            // Act & Assert
            var action = () => new TestResultService(null!);
            action.Should().Throw<ArgumentNullException>();
        }
    }
}