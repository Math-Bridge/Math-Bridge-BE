using MathBridge.Application.DTOs;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MathBridge.Application.Interfaces
{
    public interface ICenterService
    {
        Task<Guid> CreateCenterAsync(CreateCenterRequest request);
        Task UpdateCenterAsync(Guid id, UpdateCenterRequest request);
        Task DeleteCenterAsync(Guid id);
        Task<CenterDto> GetCenterByIdAsync(Guid id);
        Task<List<CenterDto>> GetAllCentersAsync();
        Task<List<CenterWithTutorsDto>> GetCentersWithTutorsAsync();
        Task<List<CenterDto>> SearchCentersAsync(CenterSearchRequest request);
        Task<int> GetCentersCountByCriteriaAsync(CenterSearchRequest request);
        Task<List<CenterDto>> GetCentersByCityAsync(string city);
        Task<List<CenterDto>> GetCentersNearLocationAsync(double latitude, double longitude, double radiusKm = 10.0);
        Task AssignTutorToCenterAsync(Guid centerId, Guid tutorId);
        Task RemoveTutorFromCenterAsync(Guid centerId, Guid tutorId);
        Task<List<TutorInCenterDto>> GetTutorsByCenterIdAsync(Guid centerId);
    }
}