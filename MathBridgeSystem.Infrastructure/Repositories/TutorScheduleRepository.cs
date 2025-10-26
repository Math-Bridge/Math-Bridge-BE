using MathBridgeSystem.Domain.Entities;
using MathBridgeSystem.Domain.Interfaces;
using MathBridgeSystem.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MathBridgeSystem.Infrastructure.Repositories
{
    public class TutorScheduleRepository : ITutorScheduleRepository
    {
        private readonly MathBridgeDbContext _context;

        public TutorScheduleRepository(MathBridgeDbContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        public async Task<TutorSchedule> GetByIdAsync(Guid availabilityId)
        {
            return await _context.TutorSchedules
                .Include(ta => ta.Tutor)
                    .ThenInclude(t => t.Role)
                .Include(ta => ta.Tutor)
                    .ThenInclude(t => t.TutorVerification)
                .FirstOrDefaultAsync(ta => ta.AvailabilityId == availabilityId);
        }

        public async Task<List<TutorSchedule>> GetByTutorIdAsync(Guid tutorId)
        {
            return await _context.TutorSchedules
                .Include(ta => ta.Tutor)
                .Where(ta => ta.TutorId == tutorId)
                .OrderBy(ta => ta.DaysOfWeek)
                .ThenBy(ta => ta.AvailableFrom)
                .ToListAsync();
        }

        public async Task<List<TutorSchedule>> GetByTutorAndDayAsync(Guid tutorId, byte daysOfWeek, DateOnly effectiveFrom, DateOnly? effectiveUntil)
        {
            var query = _context.TutorSchedules
                .Where(ta => ta.TutorId == tutorId)
                .Where(ta => (ta.DaysOfWeek & daysOfWeek) > 0)
                .Where(ta => ta.Status == "active");

            // Check for effective date overlap
            if (effectiveUntil.HasValue)
            {
                query = query.Where(ta => 
                    ta.EffectiveFrom <= effectiveUntil.Value &&
                    (ta.EffectiveUntil == null || ta.EffectiveUntil >= effectiveFrom));
            }
            else
            {
                query = query.Where(ta => 
                    ta.EffectiveUntil == null || ta.EffectiveUntil >= effectiveFrom);
            }

            return await query
                .OrderBy(ta => ta.AvailableFrom)
                .ToListAsync();
        }

        public async Task<List<TutorSchedule>> GetActiveTutorSchedulesAsync(Guid tutorId)
        {
            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            
            return await _context.TutorSchedules
                .Include(ta => ta.Tutor)
                .Where(ta => ta.TutorId == tutorId)
                .Where(ta => ta.Status == "active")
                .Where(ta => ta.EffectiveFrom <= today)
                .Where(ta => ta.EffectiveUntil == null || ta.EffectiveUntil >= today)
                .OrderBy(ta => ta.DaysOfWeek)
                .ThenBy(ta => ta.AvailableFrom)
                .ToListAsync();
        }

        public async Task<List<TutorSchedule>> GetByDaysOfWeekAsync(byte daysOfWeek, DateTime? effectiveDate = null)
        {
            var query = _context.TutorSchedules
                .Include(ta => ta.Tutor)
                    .ThenInclude(t => t.Role)
                .Where(ta => (ta.DaysOfWeek & daysOfWeek) > 0)
                .Where(ta => ta.Status == "active");

            if (effectiveDate.HasValue)
            {
                var date = DateOnly.FromDateTime(effectiveDate.Value);
                query = query.Where(ta => ta.EffectiveFrom <= date)
                            .Where(ta => ta.EffectiveUntil == null || ta.EffectiveUntil >= date);
            }

            return await query
                .OrderBy(ta => ta.AvailableFrom)
                .ToListAsync();
        }

        public async Task<List<TutorSchedule>> SearchAvailableTutorsAsync(
            byte? dayOfWeek, 
            TimeOnly? startTime, 
            TimeOnly? endTime, 
            bool? canTeachOnline, 
            bool? canTeachOffline, 
            DateTime? effectiveDate)
        {
            // Start with base query - only required filters
            var query = _context.TutorSchedules
                .Include(ta => ta.Tutor)
                .ThenInclude(t => t.Role)
                .Include(ta => ta.Tutor)
                .ThenInclude(t => t.TutorVerification)
                .Where(ta => ta.Status == "active");
            
            if (dayOfWeek.HasValue)
            {
                query = query.Where(ta => ta.DaysOfWeek == dayOfWeek.Value);
            }

            // Apply date filter only if provided
            if (effectiveDate.HasValue)
            {
                var date = DateOnly.FromDateTime(effectiveDate.Value);
                query = query
                    .Where(ta => ta.EffectiveFrom <= date)
                    .Where(ta => ta.EffectiveUntil == null || ta.EffectiveUntil >= date);
            }

            // Apply time filter only if both start and end times are provided
            if (startTime.HasValue && endTime.HasValue)
            {
                query = query.Where(ta => 
                    ta.AvailableFrom <= startTime.Value && 
                    ta.AvailableUntil >= endTime.Value);
            }

            // Apply teaching mode filters if provided
            if (canTeachOnline.HasValue)
            {
                query = query.Where(ta => ta.CanTeachOnline == canTeachOnline.Value);
            }

            if (canTeachOffline.HasValue)
            {
                query = query.Where(ta => ta.CanTeachOffline == canTeachOffline.Value);
            }

            // Execute query with ordering
            return await query
                .OrderBy(ta => ta.Tutor.FullName)
                .ThenBy(ta => ta.AvailableFrom)
                .ToListAsync();
        }


        public async Task<TutorSchedule> CreateAsync(TutorSchedule availability)
        {
            availability.AvailabilityId = Guid.NewGuid();
            availability.CreatedDate = DateTime.UtcNow;
            availability.Status = "active";

            await _context.TutorSchedules.AddAsync(availability);
            await _context.SaveChangesAsync();

            return availability;
        }

        public async Task UpdateAsync(TutorSchedule availability)
        {
            availability.UpdatedDate = DateTime.UtcNow;
            _context.TutorSchedules.Update(availability);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(Guid availabilityId)
        {
            var availability = await _context.TutorSchedules
                .FirstOrDefaultAsync(ta => ta.AvailabilityId == availabilityId);

            if (availability != null)
            {
                _context.TutorSchedules.Remove(availability);
                await _context.SaveChangesAsync();
            }
        }

        public async Task<bool> HasConflictAsync(
            Guid tutorId, 
            byte daysOfWeek, 
            TimeOnly startTime, 
            TimeOnly endTime, 
            DateOnly effectiveFrom, 
            DateOnly? effectiveUntil, 
            Guid? excludeAvailabilityId = null)
        {
            var query = _context.TutorSchedules
                .Where(ta => ta.TutorId == tutorId)
                .Where(ta => (ta.DaysOfWeek & daysOfWeek) > 0)
                .Where(ta => ta.Status == "active");

            // Exclude specific availability (for updates)
            if (excludeAvailabilityId.HasValue)
            {
                query = query.Where(ta => ta.AvailabilityId != excludeAvailabilityId.Value);
            }

            // Check for time overlap: (start < existing_end AND end > existing_start)
            query = query.Where(ta => startTime < ta.AvailableUntil && endTime > ta.AvailableFrom);

            // Check for effective date range overlap
            // Ranges overlap if: start1 <= end2 AND end1 >= start2
            if (effectiveUntil.HasValue)
            {
                query = query.Where(ta => 
                    effectiveFrom <= (ta.EffectiveUntil ?? DateOnly.MaxValue) &&
                    effectiveUntil.Value >= ta.EffectiveFrom);
            }
            else
            {
                // If no end date, check if it overlaps with any existing availability
                query = query.Where(ta => 
                    ta.EffectiveUntil == null || ta.EffectiveUntil >= effectiveFrom);
            }

            return await query.AnyAsync();
        }
    }
}