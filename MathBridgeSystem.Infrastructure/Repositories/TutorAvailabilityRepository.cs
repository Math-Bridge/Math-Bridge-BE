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
    public class TutorAvailabilityRepository : ITutorAvailabilityRepository
    {
        private readonly MathBridgeDbContext _context;

        public TutorAvailabilityRepository(MathBridgeDbContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        public async Task<TutorAvailability> GetByIdAsync(Guid availabilityId)
        {
            return await _context.TutorAvailabilities
                .Include(ta => ta.Tutor)
                    .ThenInclude(t => t.Role)
                .Include(ta => ta.Tutor)
                    .ThenInclude(t => t.TutorVerification)
                .FirstOrDefaultAsync(ta => ta.AvailabilityId == availabilityId);
        }

        public async Task<List<TutorAvailability>> GetByTutorIdAsync(Guid tutorId)
        {
            return await _context.TutorAvailabilities
                .Include(ta => ta.Tutor)
                .Where(ta => ta.TutorId == tutorId)
                .OrderBy(ta => ta.DayOfWeek)
                .ThenBy(ta => ta.AvailableFrom)
                .ToListAsync();
        }

        public async Task<List<TutorAvailability>> GetActiveTutorAvailabilitiesAsync(Guid tutorId)
        {
            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            
            return await _context.TutorAvailabilities
                .Include(ta => ta.Tutor)
                .Where(ta => ta.TutorId == tutorId)
                .Where(ta => ta.Status == "active")
                .Where(ta => ta.EffectiveFrom <= today)
                .Where(ta => ta.EffectiveUntil == null || ta.EffectiveUntil >= today)
                .OrderBy(ta => ta.DayOfWeek)
                .ThenBy(ta => ta.AvailableFrom)
                .ToListAsync();
        }

        public async Task<List<TutorAvailability>> GetByDayOfWeekAsync(int dayOfWeek, DateTime? effectiveDate = null)
        {
            var query = _context.TutorAvailabilities
                .Include(ta => ta.Tutor)
                    .ThenInclude(t => t.Role)
                .Where(ta => ta.DayOfWeek == dayOfWeek)
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

        public async Task<List<TutorAvailability>> SearchAvailableTutorsAsync(
            int? dayOfWeek, 
            TimeOnly? startTime, 
            TimeOnly? endTime, 
            bool? canTeachOnline, 
            bool? canTeachOffline, 
            DateTime? effectiveDate)
        {
            // Start with base query - only required filters
            var query = _context.TutorAvailabilities
                .Include(ta => ta.Tutor)
                .ThenInclude(t => t.Role)
                .Include(ta => ta.Tutor)
                .ThenInclude(t => t.TutorVerification)
                .Where(ta => ta.Status == "active")
                .Where(ta => ta.CurrentBookings < ta.MaxConcurrentBookings);
            
            if (dayOfWeek.HasValue)
            {
                query = query.Where(ta => ta.DayOfWeek == dayOfWeek.Value);
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


        public async Task<TutorAvailability> CreateAsync(TutorAvailability availability)
        {
            availability.AvailabilityId = Guid.NewGuid();
            availability.CreatedDate = DateTime.UtcNow;
            availability.Status = "active";
            availability.CurrentBookings = 0;

            await _context.TutorAvailabilities.AddAsync(availability);
            await _context.SaveChangesAsync();

            return availability;
        }

        public async Task UpdateAsync(TutorAvailability availability)
        {
            availability.UpdatedDate = DateTime.UtcNow;
            _context.TutorAvailabilities.Update(availability);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(Guid availabilityId)
        {
            var availability = await _context.TutorAvailabilities
                .FirstOrDefaultAsync(ta => ta.AvailabilityId == availabilityId);

            if (availability != null)
            {
                _context.TutorAvailabilities.Remove(availability);
                await _context.SaveChangesAsync();
            }
        }

        public async Task<bool> HasConflictAsync(
            Guid tutorId, 
            int dayOfWeek, 
            TimeOnly startTime, 
            TimeOnly endTime, 
            DateOnly effectiveFrom, 
            DateOnly? effectiveUntil, 
            Guid? excludeAvailabilityId = null)
        {
            var query = _context.TutorAvailabilities
                .Where(ta => ta.TutorId == tutorId)
                .Where(ta => ta.DayOfWeek == dayOfWeek)
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

        public async Task UpdateBookingCountAsync(Guid availabilityId, int increment)
        {
            var availability = await _context.TutorAvailabilities
                .FirstOrDefaultAsync(ta => ta.AvailabilityId == availabilityId);

            if (availability != null)
            {
                availability.CurrentBookings += increment;
                
                // Ensure current bookings doesn't go negative
                if (availability.CurrentBookings < 0)
                {
                    availability.CurrentBookings = 0;
                }

                // Ensure current bookings doesn't exceed max
                if (availability.CurrentBookings > availability.MaxConcurrentBookings)
                {
                    throw new InvalidOperationException(
                        $"Cannot exceed maximum concurrent bookings ({availability.MaxConcurrentBookings})");
                }

                availability.UpdatedDate = DateTime.UtcNow;
                await _context.SaveChangesAsync();
            }
        }
    }
}