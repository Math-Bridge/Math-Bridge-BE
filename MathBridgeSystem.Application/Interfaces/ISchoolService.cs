
using MathBridge.Domain.Entities;
using MathBridgeSystem.Application.DTOs;

namespace MathBridgeSystem.Application.Interfaces
{
    public interface ISchoolService
    {
        Task<School> CreateSchoolAsync(CreateSchoolRequest request);
        Task<SchoolResponse> GetSchoolByIdAsync(Guid id);
        Task<IEnumerable<SchoolResponse>> GetAllSchoolsAsync();
        Task<School> UpdateSchoolAsync(Guid id, UpdateSchoolRequest request);
        Task DeleteSchoolAsync(Guid id);
    }
}