
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

/// <summary>
        /// Searches for schools within a specified radius (in km) from the current user's location.
        /// </summary>
        /// <param name="userId">The ID of the current user.</param>
        /// <param name="radiusKm">The search radius in kilometers.</param>
        /// <returns>A list of schools within the radius.</returns>
        Task<IEnumerable<SchoolResponse>> SearchByRadiusAsync(Guid userId, double radiusKm);
    }
}