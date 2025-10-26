using MathBridgeSystem.Application.DTOs.TutorSchedule;
using MathBridgeSystem.Application.Interfaces;
using MathBridgeSystem.Domain.Entities;
using MathBridgeSystem.Domain.Interfaces;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;

namespace MathBridgeSystem.Application.Services
{
    public class TutorScheduleService : ITutorScheduleService
    {
        private readonly ITutorScheduleRepository _availabilityRepository;
        private readonly IUserRepository _userRepository;

        public TutorScheduleService(
            ITutorScheduleRepository availabilityRepository,
            IUserRepository userRepository)
        {
            _availabilityRepository = availabilityRepository ?? throw new ArgumentNullException(nameof(availabilityRepository));
            _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
        }

        public async Task<Guid> CreateAvailabilityAsync(CreateTutorScheduleRequest request)
        {
            // Validate tutor exists and has tutor role
            var tutor = await _userRepository.GetTutorWithVerificationAsync(request.TutorId);
            if (tutor == null)
            {
                throw new Exception("Tutor not found or user is not a tutor");
            }

            // Validate tutor is verified
            if (tutor.TutorVerification == null || tutor.TutorVerification.VerificationStatus != "approved")
            {
                throw new Exception("Tutor must be verified before creating availability");
            }

            // Validate day of week range
            if (request.DaysOfWeek < 0 || request.DaysOfWeek > 127)
            {
                throw new ArgumentException("Day of week must be between 0 (Sunday) and 6 (Saturday)");
            }

            if (request.DaysOfWeek == 0) {
                throw new ArgumentException(\"At least one day must be selected\");
            }

            // Validate time ranges
            if (request.AvailableUntil <= request.AvailableFrom)
            {
                throw new ArgumentException("Available until time must be after available from time");
            }
            
            if (request.AvailableFrom < TimeOnly.FromTimeSpan(new TimeSpan(16, 0, 0)) ||
                request.AvailableUntil > TimeOnly.FromTimeSpan(new TimeSpan(22, 0, 0)))
            {
                throw new ArgumentException("Available time must be between 16:00 and 22:00");
            }

            // Validate time slot duration (must be between 1.5 and 2 hours)
            var duration = request.AvailableUntil.ToTimeSpan() - request.AvailableFrom.ToTimeSpan();
            var durationMinutes = duration.TotalMinutes;
            if (durationMinutes < 90 || durationMinutes > 120)
            {
                throw new ArgumentException("Time slot duration must be between 1.5 hours (90 minutes) and 2 hours (120 minutes)");
            }

            // Check for minimum 15-minute spacing with existing time slots
            var existingSlots = await _availabilityRepository.GetByTutorIdAsync(request.TutorId);
            var slotsOnSameDay = existingSlots
                .Where(a => a.DaysOfWeek == request.DaysOfWeek 
                    && a.Status == "active"
                    && (!request.EffectiveUntil.HasValue || !a.EffectiveUntil.HasValue || 
                        a.EffectiveFrom <= request.EffectiveUntil.Value)
                    && (a.EffectiveUntil == null || a.EffectiveUntil.Value >= request.EffectiveFrom))
                .ToList();

            foreach (var existingSlot in slotsOnSameDay)
            {
                // Only check if new slot starts after existing slot ends
                if (request.AvailableFrom.ToTimeSpan() >= existingSlot.AvailableUntil.ToTimeSpan())
                {
                    var gapAfterExisting = request.AvailableFrom.ToTimeSpan() - existingSlot.AvailableUntil.ToTimeSpan();
                    if (gapAfterExisting.TotalMinutes < 15)
                    {
                        throw new ArgumentException($"Time slot start must be at least 15 minutes after the previous slot ends. Conflict with slot {existingSlot.AvailableFrom:HH:mm}-{existingSlot.AvailableUntil:HH:mm}");
                    }
                }
                // If there's any overlap, it will be caught by the conflict check below
            }

            // Validate effective date ranges
            if (request.EffectiveUntil.HasValue && request.EffectiveUntil.Value <= request.EffectiveFrom)
            {
                throw new ArgumentException("Effective until date must be after effective from date");
            }

            // Validate at least one teaching mode is enabled
            if (!request.CanTeachOnline && !request.CanTeachOffline)
            {
                throw new ArgumentException("At least one teaching mode (online or offline) must be enabled");
            }

            // Validate max concurrent bookings
            if (request.MaxConcurrentBookings < 1 || request.MaxConcurrentBookings > 10)
            {
                throw new ArgumentException("Max concurrent bookings must be between 1 and 10");
            }

                        // Check for conflicts
            var hasConflict = await _availabilityRepository.HasConflictAsync(
                request.TutorId,
                request.DaysOfWeek,
                request.AvailableFrom,
                request.AvailableUntil,
                request.EffectiveFrom,
                request.EffectiveUntil);

            if (hasConflict)
            {
                throw new Exception("This availability conflicts with an existing time slot");
            }

            // Create entity
            var availability = new TutorSchedule
            {
                TutorId = request.TutorId,
                DaysOfWeek = request.DaysOfWeek,
                AvailableFrom = request.AvailableFrom,
                AvailableUntil = request.AvailableUntil,
                EffectiveFrom = request.EffectiveFrom,
                EffectiveUntil = request.EffectiveUntil,
                CanTeachOnline = request.CanTeachOnline,
                CanTeachOffline = request.CanTeachOffline
            };

            var created = await _availabilityRepository.CreateAsync(availability);
            return created.AvailabilityId;
        }

        public async Task UpdateAvailabilityAsync(Guid availabilityId, UpdateTutorScheduleRequest request)
        {
            var availability = await _availabilityRepository.GetByIdAsync(availabilityId);
            if (availability == null)
            {
                throw new Exception("Availability not found");
            }

            // Update only provided fields, preserve old data if null
            if (request.DaysOfWeek.HasValue)
            {
                if (request.DaysOfWeek.Value < 0 || request.DaysOfWeek.Value > 6)
                {
                    throw new ArgumentException("Day of week must be between 0 (Sunday) and 6 (Saturday)");
                }
                availability.DaysOfWeek = request.DaysOfWeek.Value;
            }

            if (request.AvailableFrom.HasValue)
            {
                availability.AvailableFrom = request.AvailableFrom.Value;
            }

            if (request.AvailableUntil.HasValue)
            {
                availability.AvailableUntil = request.AvailableUntil.Value;
            }

            if (request.DaysOfWeek == 0) {
                throw new ArgumentException(\"At least one day must be selected\");
            }

            // Validate time ranges after update
            if (availability.AvailableUntil <= availability.AvailableFrom)
            {
                throw new ArgumentException("Available until time must be after available from time");
            }

            // Validate time slot duration (must be between 1.5 and 2 hours) - same as CreateAvailability
            var duration = availability.AvailableUntil.ToTimeSpan() - availability.AvailableFrom.ToTimeSpan();
            var durationMinutes = duration.TotalMinutes;
            if (durationMinutes < 90 || durationMinutes > 120)
            {
                throw new ArgumentException("Time slot duration must be between 1.5 hours (90 minutes) and 2 hours (120 minutes)");
            }

            if (request.EffectiveFrom.HasValue)
            {
                availability.EffectiveFrom = request.EffectiveFrom.Value;
            }

            if (request.EffectiveUntil.HasValue)
            {
                availability.EffectiveUntil = request.EffectiveUntil.Value;
            }

            // Validate effective date ranges after update
            if (availability.EffectiveUntil.HasValue && availability.EffectiveUntil.Value <= availability.EffectiveFrom)
            {
                throw new ArgumentException("Effective until date must be after effective from date");
            }
            

            if (request.CanTeachOnline.HasValue)
            {
                availability.CanTeachOnline = request.CanTeachOnline.Value;
            }

            if (request.CanTeachOffline.HasValue)
            {
                availability.CanTeachOffline = request.CanTeachOffline.Value;
            }

            // Validate at least one teaching mode is enabled
            if (!availability.CanTeachOnline && !availability.CanTeachOffline)
            {
                throw new ArgumentException("At least one teaching mode (online or offline) must be enabled");
            }

            // Check for minimum 15-minute spacing with existing time slots (excluding current availability)
            var existingSlots = await _availabilityRepository.GetByTutorAndDayAsync(
                availability.TutorId,
                availability.DaysOfWeek,
                availability.EffectiveFrom,
                availability.EffectiveUntil);

            foreach (var slot in existingSlots.Where(s => s.AvailabilityId != availabilityId))
            {
                var timeDiff1 = Math.Abs((availability.AvailableFrom.ToTimeSpan() - slot.AvailableUntil.ToTimeSpan()).TotalMinutes);
                var timeDiff2 = Math.Abs((slot.AvailableFrom.ToTimeSpan() - availability.AvailableUntil.ToTimeSpan()).TotalMinutes);

                if (timeDiff1 < 15 || timeDiff2 < 15)
                {
                    throw new Exception($"Time slots must have at least 15 minutes spacing. Conflict with slot {slot.AvailableFrom:HH:mm}-{slot.AvailableUntil:HH:mm}");
                }
            }

            // Check for conflicts (excluding current availability)
            var hasConflict = await _availabilityRepository.HasConflictAsync(
                availability.TutorId,
                availability.DaysOfWeek,
                availability.AvailableFrom,
                availability.AvailableUntil,
                availability.EffectiveFrom,
                availability.EffectiveUntil,
                availabilityId);

            if (hasConflict)
            {
                throw new Exception("Updated availability conflicts with an existing time slot");
            }

            await _availabilityRepository.UpdateAsync(availability);
        }

        public async Task DeleteAvailabilityAsync(Guid availabilityId)
        {
            var availability = await _availabilityRepository.GetByIdAsync(availabilityId);
            if (availability == null)
            {
                throw new Exception("Availability not found");
            }

            // Prevent deletion if there are active bookings
            if (availability.IsBooked == true)
            {
                throw new Exception($"Cannot delete availability with active bookings");
            }

            await _availabilityRepository.DeleteAsync(availabilityId);
        }

        public async Task<TutorScheduleResponse> GetAvailabilityByIdAsync(Guid availabilityId)
        {
            var availability = await _availabilityRepository.GetByIdAsync(availabilityId);
            if (availability == null)
            {
                return null;
            }

            return MapToResponse(availability);
        }

        public async Task<List<TutorScheduleResponse>> GetTutorSchedulesAsync(Guid tutorId, bool activeOnly = true)
        {
            List<TutorSchedule> availabilities;

            if (activeOnly)
            {
                availabilities = await _availabilityRepository.GetActiveTutorSchedulesAsync(tutorId);
            }
            else
            {
                availabilities = await _availabilityRepository.GetByTutorIdAsync(tutorId);
            }

            return availabilities.Select(MapToResponse).ToList();
        }

        public async Task<List<AvailableTutorResponse>> SearchAvailableTutorsAsync(SearchAvailableTutorsRequest request)
        {
            // Validate search parameters
            if (request.DaysOfWeek < 0 || request.DaysOfWeek > 127)
            {
                throw new ArgumentException("Day of week must be between 0 (Sunday) and 6 (Saturday)");
            }

            if (request.EndTime <= request.StartTime && request.EndTime != request.StartTime)
            {
                throw new ArgumentException("End time must be after start time");
            }

            if (request.Page < 1)
            {
                request.Page = 1;
            }

            if (request.PageSize < 1 || request.PageSize > 50)
            {
                request.PageSize = 20;
            }

            var availabilities = await _availabilityRepository.SearchAvailableTutorsAsync(
                request.DaysOfWeek,
                request.StartTime,
                request.EndTime,
                request.CanTeachOnline,
                request.CanTeachOffline,
                request.EffectiveDate);

            // Group by tutor and create aggregated response
            var tutorGroups = availabilities
                .GroupBy(a => a.TutorId)
                .Select(group => new AvailableTutorResponse
                {
                    TutorId = group.Key,
                    TutorName = group.First().Tutor.FullName,
                    TutorEmail = group.First().Tutor.Email,
                    AvailabilitySlots = group.Select(MapToResponse).ToList(),
                    CanTeachOnline = group.Any(a => a.CanTeachOnline),
                    CanTeachOffline = group.Any(a => a.CanTeachOffline)
                })
                .OrderByDescending(t => t.TotalAvailableSlots)
                .ThenBy(t => t.TutorName);

            // Apply pagination
            var pagedResults = tutorGroups
                .Skip((request.Page - 1) * request.PageSize)
                .Take(request.PageSize)
                .ToList();

            return pagedResults;
        }



        public async Task UpdateAvailabilityStatusAsync(Guid availabilityId, string status)
        {
            var availability = await _availabilityRepository.GetByIdAsync(availabilityId);
            if (availability == null)
            {
                throw new Exception("Availability not found");
            }

            // Validate status
            var validStatuses = new[] { "active", "inactive", "deleted" };
            if (!validStatuses.Contains(status.ToLower()))
            {
                throw new ArgumentException($"Invalid status. Must be one of: {string.Join(", ", validStatuses)}");
            }

            availability.Status = status.ToLower();
            await _availabilityRepository.UpdateAsync(availability);
        }
        

        public async Task<List<Guid>> BulkCreateAvailabilitiesAsync(List<CreateTutorScheduleRequest> requests)
        {
            var createdIds = new List<Guid>();

            foreach (var request in requests)
            {
                try
                {
                    var id = await CreateAvailabilityAsync(request);
                    createdIds.Add(id);
                }
                catch (Exception ex)
                {
                    // Log error but continue with other requests
                    // In production, consider more sophisticated error handling
                    throw new Exception($"Failed to create availability for day {request.DaysOfWeek}: {ex.Message}");
                }
            }

            return createdIds;
        }

        private TutorScheduleResponse MapToResponse(TutorSchedule availability)
        {
            return new TutorScheduleResponse
            {
                AvailabilityId = availability.AvailabilityId,
                TutorId = availability.TutorId,
                TutorName = availability.Tutor?.FullName ?? "Unknown",
                DaysOfWeeks = availability.DaysOfWeek,
                DaysOfWeeksName= GetDaysOfWeekName(availability.DaysOfWeek),
                AvailableFrom = availability.AvailableFrom,
                AvailableUntil = availability.AvailableUntil,
                EffectiveFrom = availability.EffectiveFrom,
                EffectiveUntil = availability.EffectiveUntil,
                CanTeachOnline = availability.CanTeachOnline,
                CanTeachOffline = availability.CanTeachOffline,
                Status = availability.Status,
                CreatedDate = availability.CreatedDate,
                UpdatedDate = availability.UpdatedDate
            };
        }

        }
}