using MathBridgeSystem.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MathBridgeSystem.Domain.Interfaces
{
    public interface ITutorScheduleRepository
    {
        Task<TutorSchedule> GetByIdAsync(Guid availabilityId);
        Task<List<TutorSchedule>> GetByTutorIdAsync(Guid tutorId);
        Task<List<TutorSchedule>> GetByTutorAndDayAsync(Guid tutorId, byte daysOfWeek, DateOnly effectiveFrom, DateOnly? effectiveUntil);
        Task<List<TutorSchedule>> GetActiveTutorSchedulesAsync(Guid tutorId);
        Task<List<TutorSchedule>> GetByDaysOfWeekAsync(byte daysOfWeek, DateTime? effectiveDate = null);
        Task<List<TutorSchedule>> SearchAvailableTutorsAsync(int? dayOfWeek, TimeOnly? startTime, TimeOnly? endTime, bool? canTeachOnline, bool? canTeachOffline, DateTime? effectiveDate);
        Task<TutorSchedule> CreateAsync(TutorSchedule availability);
        Task UpdateAsync(TutorSchedule availability);
        Task DeleteAsync(Guid availabilityId);
        Task<bool> HasConflictAsync(Guid tutorId, byte daysOfWeek, TimeOnly startTime, TimeOnly endTime, DateOnly effectiveFrom, DateOnly? effectiveUntil, Guid? excludeAvailabilityId = null);
    }
}