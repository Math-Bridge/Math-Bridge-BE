using MathBridgeSystem.Application.DTOs.TestResult;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MathBridgeSystem.Application.Interfaces
{
    public interface ITestResultService
    {
        Task<TestResultDto> GetTestResultByIdAsync(Guid resultId);
        Task<IEnumerable<TestResultDto>> GetTestResultsByContractIdAsync(Guid contractId);
        Task<IEnumerable<TestResultDto>> GetTestResultsByChildIdAsync(Guid childId);
        Task<Guid> CreateTestResultAsync(CreateTestResultRequest request);
        Task UpdateTestResultAsync(Guid resultId, UpdateTestResultRequest request);
        Task<bool> DeleteTestResultAsync(Guid resultId);
    }
}

