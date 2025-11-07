using MathBridgeSystem.Domain.Entities;

namespace MathBridgeSystem.Domain.Interfaces
{
    public interface ITestResultRepository
    {
        Task<TestResult> GetByIdAsync(Guid id);
        Task<IEnumerable<TestResult>> GetByTutorIdAsync(Guid tutorId);
        Task<IEnumerable<TestResult>> GetByChildIdAsync(Guid childId);
        Task<IEnumerable<TestResult>> GetByCurriculumIdAsync(Guid curriculumId);
        Task<TestResult> AddAsync(TestResult testResult);
        Task<TestResult> UpdateAsync(TestResult testResult);
        Task<bool> DeleteAsync(Guid id);
    }
}
