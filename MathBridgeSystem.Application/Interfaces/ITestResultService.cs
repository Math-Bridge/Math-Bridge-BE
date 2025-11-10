using MathBridgeSystem.Application.DTOs.TestResult;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MathBridgeSystem.Application.Interfaces
{
    public interface ITestResultService
    {
        Task<TestResultDto> GetTestResultByIdAsync(Guid resultId);
        Task<IEnumerable<TestResultDto>> GetTestResultsByTutorIdAsync(Guid tutorId);
        Task<IEnumerable<TestResultDto>> GetTestResultsByChildIdAsync(Guid childId);
        Task<IEnumerable<TestResultDto>> GetTestResultsByCurriculumIdAsync(Guid curriculumId);
        Task<Guid> CreateTestResultAsync(CreateTestResultRequest request, Guid tutorId);
        Task UpdateTestResultAsync(Guid resultId, UpdateTestResultRequest request, Guid tutorId);
        Task<bool> DeleteTestResultAsync(Guid resultId, Guid tutorId);
        Task<TestResultStatisticsDto> GetChildStatisticsAsync(Guid childId);
    }
}