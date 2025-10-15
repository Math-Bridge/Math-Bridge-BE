using MathBridgeSystem.Application.DTOs.TutorAvailability;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MathBridgeSystem.Application.Interfaces
{
    public interface ITutorAvailabilityService
    {
        Task<Guid> CreateAvailabilityAsync(CreateTutorAvailabilityRequest request);
        Task UpdateAvailabilityAsync(Guid availabilityId, UpdateTutorAvailabilityRequest request);
        Task DeleteAvailabilityAsync(Guid availabilityId);
        Task<TutorAvailabilityResponse> GetAvailabilityByIdAsync(Guid availabilityId);
        Task<List<TutorAvailabilityResponse>> GetTutorAvailabilitiesAsync(Guid tutorId, bool activeOnly = true);
        Task<List<AvailableTutorResponse>> SearchAvailableTutorsAsync(SearchAvailableTutorsRequest request);
        Task UpdateAvailabilityStatusAsync(Guid availabilityId, string status);
        Task IncrementBookingCountAsync(Guid availabilityId);
        Task DecrementBookingCountAsync(Guid availabilityId);
        Task<List<Guid>> BulkCreateAvailabilitiesAsync(List<CreateTutorAvailabilityRequest> requests);
    }
}