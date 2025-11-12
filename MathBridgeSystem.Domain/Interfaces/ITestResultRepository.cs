using MathBridgeSystem.Domain.Entities;

namespace MathBridgeSystem.Domain.Interfaces
{
    public interface ITestResultRepository
    {
        Task<TestResult> GetByIdAsync(Guid id);
        Task<IEnumerable<TestResult>> GetByContractIdAsync(Guid contractId);
        Task<TestResult> AddAsync(TestResult testResult);
        Task<TestResult> UpdateAsync(TestResult testResult);
        Task<bool> DeleteAsync(Guid id);
    }
}
