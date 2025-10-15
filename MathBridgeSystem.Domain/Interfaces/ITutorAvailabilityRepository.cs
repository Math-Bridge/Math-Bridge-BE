using MathBridgeSystem.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MathBridgeSystem.Domain.Interfaces
{
    public interface ITutorAvailabilityRepository
    {
        Task<TutorAvailability> GetByIdAsync(Guid availabilityId);
        Task<List<TutorAvailability>> GetByTutorIdAsync(Guid tutorId);
        Task<List<TutorAvailability>> GetByTutorAndDayAsync(Guid tutorId, int dayOfWeek, DateOnly effectiveFrom, DateOnly? effectiveUntil);
        Task<List<TutorAvailability>> GetActiveTutorAvailabilitiesAsync(Guid tutorId);
        Task<List<TutorAvailability>> GetByDayOfWeekAsync(int dayOfWeek, DateTime? effectiveDate = null);
        Task<List<TutorAvailability>> SearchAvailableTutorsAsync(int? dayOfWeek, TimeOnly? startTime, TimeOnly? endTime, bool? canTeachOnline, bool? canTeachOffline, DateTime? effectiveDate);
        Task<TutorAvailability> CreateAsync(TutorAvailability availability);
        Task UpdateAsync(TutorAvailability availability);
        Task DeleteAsync(Guid availabilityId);
        Task<bool> HasConflictAsync(Guid tutorId, int dayOfWeek, TimeOnly startTime, TimeOnly endTime, DateOnly effectiveFrom, DateOnly? effectiveUntil, Guid? excludeAvailabilityId = null);
        Task UpdateBookingCountAsync(Guid availabilityId, int increment);
    }
}