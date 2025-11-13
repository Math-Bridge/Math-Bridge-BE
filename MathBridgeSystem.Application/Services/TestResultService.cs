using MathBridgeSystem.Application.DTOs.TestResult;
using MathBridgeSystem.Application.Interfaces;
using MathBridgeSystem.Domain.Entities;
using MathBridgeSystem.Domain.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
namespace MathBridgeSystem.Application.Services
{
    public class TestResultService : ITestResultService
    {
        private readonly ITestResultRepository _testResultRepository;
        public TestResultService(ITestResultRepository testResultRepository)
        {
            _testResultRepository = testResultRepository ?? throw new ArgumentNullException(nameof(testResultRepository));
        }
        public async Task<TestResultDto> GetTestResultByIdAsync(Guid resultId)
        {
            var testResult = await _testResultRepository.GetByIdAsync(resultId);
            if (testResult == null)
                throw new KeyNotFoundException($"Test result with ID {resultId} not found.");
            return MapToDto(testResult);
        }
        public async Task<IEnumerable<TestResultDto>> GetTestResultsByContractIdAsync(Guid contractId)
        {
            var testResults = await _testResultRepository.GetByContractIdAsync(contractId);
            return testResults.Select(MapToDto);
        }
        public async Task<Guid> CreateTestResultAsync(CreateTestResultRequest request)
        {
            var testResult = new TestResult
            {
                ResultId = Guid.NewGuid(),
                TestType = request.TestType,
                Score = request.Score,
                Notes = request.Notes,
                ContractId = request.ContractId,
                CreatedDate = DateTime.UtcNow.ToLocalTime(),
                UpdatedDate = DateTime.UtcNow.ToLocalTime()
            };
            var createdTestResult = await _testResultRepository.AddAsync(testResult);
            return createdTestResult.ResultId;
        }
        public async Task UpdateTestResultAsync(Guid resultId, UpdateTestResultRequest request)
        {
            var testResult = await _testResultRepository.GetByIdAsync(resultId);
            if (testResult == null)
                throw new KeyNotFoundException($"Test result with ID {resultId} not found.");
            if (!string.IsNullOrEmpty(request.TestType))
                testResult.TestType = request.TestType;
            if (request.Score.HasValue)
                testResult.Score = request.Score.Value;
            if (request.Notes != null)
                testResult.Notes = request.Notes;
            if (request.ContractId.HasValue)
                testResult.ContractId = request.ContractId.Value;
            testResult.UpdatedDate = DateTime.UtcNow.ToLocalTime();
            await _testResultRepository.UpdateAsync(testResult);
        }
        public async Task<bool> DeleteTestResultAsync(Guid resultId)
        {
            var testResult = await _testResultRepository.GetByIdAsync(resultId);
            if (testResult == null)
                throw new KeyNotFoundException($"Test result with ID {resultId} not found.");
            return await _testResultRepository.DeleteAsync(resultId);
        }
        private TestResultDto MapToDto(TestResult testResult)
        {
            return new TestResultDto
            {
                ResultId = testResult.ResultId,
                TestType = testResult.TestType,
                Score = testResult.Score,
                Notes = testResult.Notes,
                CreatedDate = testResult.CreatedDate,
                UpdatedDate = testResult.UpdatedDate,
                ContractId = testResult.ContractId
            };
        }
    }
}
