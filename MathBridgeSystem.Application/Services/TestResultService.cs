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
        private readonly IUserRepository _userRepository;
        private readonly IChildRepository _childRepository;
        private readonly ICurriculumRepository _curriculumRepository;

        public TestResultService(
            ITestResultRepository testResultRepository,
            IUserRepository userRepository,
            IChildRepository childRepository,
            ICurriculumRepository curriculumRepository)
        {
            _testResultRepository = testResultRepository ?? throw new ArgumentNullException(nameof(testResultRepository));
            _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
            _childRepository = childRepository ?? throw new ArgumentNullException(nameof(childRepository));
            _curriculumRepository = curriculumRepository ?? throw new ArgumentNullException(nameof(curriculumRepository));
        }

        public async Task<TestResultDto> GetTestResultByIdAsync(Guid resultId)
        {
            var testResult = await _testResultRepository.GetByIdAsync(resultId);
            if (testResult == null)
                throw new KeyNotFoundException($"Test result with ID {resultId} not found.");

            return MapToDto(testResult);
        }

        public async Task<IEnumerable<TestResultDto>> GetTestResultsByTutorIdAsync(Guid tutorId)
        {
            var testResults = await _testResultRepository.GetByTutorIdAsync(tutorId);
            return testResults.Select(MapToDto);
        }

        public async Task<IEnumerable<TestResultDto>> GetTestResultsByChildIdAsync(Guid childId)
        {
            var testResults = await _testResultRepository.GetByChildIdAsync(childId);
            return testResults.Select(MapToDto);
        }

        public async Task<IEnumerable<TestResultDto>> GetTestResultsByCurriculumIdAsync(Guid curriculumId)
        {
            var testResults = await _testResultRepository.GetByCurriculumIdAsync(curriculumId);
            return testResults.Select(MapToDto);
        }

        public async Task<Guid> CreateTestResultAsync(CreateTestResultRequest request, Guid tutorId)
        {
            // Validate tutor exists
            var tutor = await _userRepository.GetByIdAsync(tutorId);
            if (tutor == null)
                throw new ArgumentException($"Tutor with ID {tutorId} not found.");

            // Validate child exists
            var child = await _childRepository.GetByIdAsync(request.ChildId);
            if (child == null)
                throw new ArgumentException($"Child with ID {request.ChildId} not found.");

            // Validate curriculum exists
            var curriculum = await _curriculumRepository.GetByIdAsync(request.CurriculumId);
            if (curriculum == null)
                throw new ArgumentException($"Curriculum with ID {request.CurriculumId} not found.");

            // Calculate percentage
            decimal? percentage = null;
            if (request.MaxScore > 0)
            {
                percentage = (request.Score / request.MaxScore) * 100;
            }

            var testResult = new TestResult
            {
                ResultId = Guid.NewGuid(),
                TutorId = tutorId,
                ChildId = request.ChildId,
                TestName = request.TestName,
                TestType = request.TestType,
                Score = request.Score,
                MaxScore = request.MaxScore,
                Percentage = percentage,
                DurationMinutes = request.DurationMinutes,
                NumberOfQuestions = request.NumberOfQuestions,
                CorrectAnswers = request.CorrectAnswers,
                Notes = request.Notes,
                AreasForImprovement = request.AreasForImprovement,
                TestDate = request.TestDate,
                CurriculumId = request.CurriculumId,
                CreatedDate = DateTime.UtcNow,
                UpdatedDate = DateTime.UtcNow
            };

            var createdTestResult = await _testResultRepository.AddAsync(testResult);
            return createdTestResult.ResultId;
        }

        public async Task UpdateTestResultAsync(Guid resultId, UpdateTestResultRequest request, Guid tutorId)
        {
            var testResult = await _testResultRepository.GetByIdAsync(resultId);
            if (testResult == null)
                throw new KeyNotFoundException($"Test result with ID {resultId} not found.");

            // Only allow the tutor who created the result to update it
            if (testResult.TutorId != tutorId)
                throw new UnauthorizedAccessException("You can only update test results you created.");

            // Update fields if provided
            if (!string.IsNullOrEmpty(request.TestName))
                testResult.TestName = request.TestName;

            if (!string.IsNullOrEmpty(request.TestType))
                testResult.TestType = request.TestType;

            if (request.Score.HasValue)
                testResult.Score = request.Score.Value;

            if (request.MaxScore.HasValue)
                testResult.MaxScore = request.MaxScore.Value;

            // Recalculate percentage if score or max score changed
            if (request.Score.HasValue || request.MaxScore.HasValue)
            {
                if (testResult.MaxScore > 0)
                    testResult.Percentage = (testResult.Score / testResult.MaxScore) * 100;
            }

            if (request.DurationMinutes.HasValue)
                testResult.DurationMinutes = request.DurationMinutes.Value;

            if (request.NumberOfQuestions.HasValue)
                testResult.NumberOfQuestions = request.NumberOfQuestions.Value;

            if (request.CorrectAnswers.HasValue)
                testResult.CorrectAnswers = request.CorrectAnswers.Value;

            if (request.Notes != null)
                testResult.Notes = request.Notes;

            if (request.AreasForImprovement != null)
                testResult.AreasForImprovement = request.AreasForImprovement;

            if (request.TestDate.HasValue)
                testResult.TestDate = request.TestDate.Value;

            if (request.CurriculumId.HasValue)
            {
                var curriculum = await _curriculumRepository.GetByIdAsync(request.CurriculumId.Value);
                if (curriculum == null)
                    throw new ArgumentException($"Curriculum with ID {request.CurriculumId} not found.");
                testResult.CurriculumId = request.CurriculumId.Value;
            }

            testResult.UpdatedDate = DateTime.UtcNow;
            await _testResultRepository.UpdateAsync(testResult);
        }

        public async Task<bool> DeleteTestResultAsync(Guid resultId, Guid tutorId)
        {
            var testResult = await _testResultRepository.GetByIdAsync(resultId);
            if (testResult == null)
                throw new KeyNotFoundException($"Test result with ID {resultId} not found.");

            // Only allow the tutor who created the result to delete it
            if (testResult.TutorId != tutorId)
                throw new UnauthorizedAccessException("You can only delete test results you created.");

            return await _testResultRepository.DeleteAsync(resultId);
        }

        public async Task<TestResultStatisticsDto> GetChildStatisticsAsync(Guid childId)
        {
            var testResults = await _testResultRepository.GetByChildIdAsync(childId);
            var resultsList = testResults.ToList();

            if (!resultsList.Any())
            {
                return new TestResultStatisticsDto
                {
                    ChildId = childId,
                    TotalTests = 0,
                    AverageScore = 0,
                    AveragePercentage = 0,
                    HighestScore = 0,
                    LowestScore = 0,
                    LastTestDate = null
                };
            }

            return new TestResultStatisticsDto
            {
                ChildId = childId,
                TotalTests = resultsList.Count,
                AverageScore = resultsList.Average(r => r.Score),
                AveragePercentage = resultsList.Where(r => r.Percentage.HasValue).Any() 
                    ? resultsList.Where(r => r.Percentage.HasValue).Average(r => r.Percentage!.Value) 
                    : 0,
                HighestScore = resultsList.Max(r => r.Score),
                LowestScore = resultsList.Min(r => r.Score),
                LastTestDate = resultsList.Max(r => r.TestDate)
            };
        }

        private TestResultDto MapToDto(TestResult testResult)
        {
            return new TestResultDto
            {
                ResultId = testResult.ResultId,
                TutorId = testResult.TutorId,
                TutorName = testResult.Tutor?.FullName ?? "Unknown",
                ChildId = testResult.ChildId,
                ChildName = testResult.Child?.ChildName ?? "Unknown",
                TestName = testResult.TestName,
                TestType = testResult.TestType,
                Score = testResult.Score,
                MaxScore = testResult.MaxScore,
                Percentage = testResult.Percentage,
                DurationMinutes = testResult.DurationMinutes,
                NumberOfQuestions = testResult.NumberOfQuestions,
                CorrectAnswers = testResult.CorrectAnswers,
                Notes = testResult.Notes,
                AreasForImprovement = testResult.AreasForImprovement,
                TestDate = testResult.TestDate,
                CreatedDate = testResult.CreatedDate,
                UpdatedDate = testResult.UpdatedDate,
                CurriculumId = testResult.CurriculumId,
                CurriculumName = testResult.Curriculum?.CurriculumName ?? "Unknown"
            };
        }
    }
}