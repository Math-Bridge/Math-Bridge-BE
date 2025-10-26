using MathBridgeSystem.Application.DTOs.TutorSchedule;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MathBridgeSystem.Application.Interfaces
{
    public interface ITutorScheduleService
    {
        Task<Guid> CreateAvailabilityAsync(CreateTutorScheduleRequest request);
        Task UpdateAvailabilityAsync(Guid availabilityId, UpdateTutorScheduleRequest request);
        Task DeleteAvailabilityAsync(Guid availabilityId);
        Task<TutorScheduleResponse> GetAvailabilityByIdAsync(Guid availabilityId);
        Task<List<TutorScheduleResponse>> GetTutorSchedulesAsync(Guid tutorId, bool activeOnly = true);
        Task<List<AvailableTutorResponse>> SearchAvailableTutorsAsync(SearchAvailableTutorsRequest request);
        Task UpdateAvailabilityStatusAsync(Guid availabilityId, string status);
        Task<List<Guid>> BulkCreateAvailabilitiesAsync(List<CreateTutorScheduleRequest> requests);
    }
}