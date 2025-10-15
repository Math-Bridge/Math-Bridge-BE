using MathBridgeSystem.Application.DTOs.TutorAvailability;
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
    public class TutorAvailabilityService : ITutorAvailabilityService
    {
        private readonly ITutorAvailabilityRepository _availabilityRepository;
        private readonly IUserRepository _userRepository;

        public TutorAvailabilityService(
            ITutorAvailabilityRepository availabilityRepository,
            IUserRepository userRepository)
        {
            _availabilityRepository = availabilityRepository ?? throw new ArgumentNullException(nameof(availabilityRepository));
            _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
        }

        public async Task<Guid> CreateAvailabilityAsync(CreateTutorAvailabilityRequest request)
        {
            // Validate tutor exists and has tutor role
            var tutor = await _userRepository.GetTutorWithVerificationAsync(request.TutorId);
            if (tutor == null)
            {
                throw new Exception("Tutor not found or user is not a tutor");
            }

            // Validate tutor is verified
            if (tutor.TutorVerification == null || tutor.TutorVerification.VerificationStatus != "verified")
            {
                throw new Exception("Tutor must be verified before creating availability");
            }

            // Validate day of week range
            if (request.DayOfWeek < 0 || request.DayOfWeek > 6)
            {
                throw new ArgumentException("Day of week must be between 0 (Sunday) and 6 (Saturday)");
            }

            // Validate time ranges
            if (request.AvailableUntil <= request.AvailableFrom)
            {
                throw new ArgumentException("Available until time must be after available from time");
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

            // Validate travel distance if offline teaching is enabled
            if (request.CanTeachOffline && (!request.MaxTravelDistanceKm.HasValue || request.MaxTravelDistanceKm.Value <= 0))
            {
                throw new ArgumentException("Max travel distance must be provided when offline teaching is enabled");
            }

            // Check for conflicts
            var hasConflict = await _availabilityRepository.HasConflictAsync(
                request.TutorId,
                request.DayOfWeek,
                request.AvailableFrom,
                request.AvailableUntil,
                request.EffectiveFrom,
                request.EffectiveUntil);

            if (hasConflict)
            {
                throw new Exception("This availability conflicts with an existing time slot");
            }

            // Create entity
            var availability = new TutorAvailability
            {
                TutorId = request.TutorId,
                DayOfWeek = request.DayOfWeek,
                AvailableFrom = request.AvailableFrom,
                AvailableUntil = request.AvailableUntil,
                EffectiveFrom = request.EffectiveFrom,
                EffectiveUntil = request.EffectiveUntil,
                MaxConcurrentBookings = request.MaxConcurrentBookings,
                CanTeachOnline = request.CanTeachOnline,
                CanTeachOffline = request.CanTeachOffline,
                MaxTravelDistanceKm = request.MaxTravelDistanceKm
            };

            var created = await _availabilityRepository.CreateAsync(availability);
            return created.AvailabilityId;
        }

        public async Task UpdateAvailabilityAsync(Guid availabilityId, UpdateTutorAvailabilityRequest request)
        {
            var availability = await _availabilityRepository.GetByIdAsync(availabilityId);
            if (availability == null)
            {
                throw new Exception("Availability not found");
            }

            // Update only provided fields
            if (request.DayOfWeek.HasValue)
            {
                if (request.DayOfWeek.Value < 0 || request.DayOfWeek.Value > 6)
                {
                    throw new ArgumentException("Day of week must be between 0 (Sunday) and 6 (Saturday)");
                }
                availability.DayOfWeek = request.DayOfWeek.Value;
            }

            if (request.AvailableFrom.HasValue)
            {
                availability.AvailableFrom = request.AvailableFrom.Value;
            }

            if (request.AvailableUntil.HasValue)
            {
                availability.AvailableUntil = request.AvailableUntil.Value;
            }

            // Validate time ranges after update
            if (availability.AvailableUntil <= availability.AvailableFrom)
            {
                throw new ArgumentException("Available until time must be after available from time");
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

            if (request.MaxConcurrentBookings.HasValue)
            {
                if (request.MaxConcurrentBookings.Value < 1 || request.MaxConcurrentBookings.Value > 10)
                {
                    throw new ArgumentException("Max concurrent bookings must be between 1 and 10");
                }

                // Prevent reducing max bookings below current bookings
                if (request.MaxConcurrentBookings.Value < availability.CurrentBookings)
                {
                    throw new ArgumentException($"Cannot reduce max concurrent bookings below current bookings ({availability.CurrentBookings})");
                }

                availability.MaxConcurrentBookings = request.MaxConcurrentBookings.Value;
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

            if (request.MaxTravelDistanceKm.HasValue)
            {
                availability.MaxTravelDistanceKm = request.MaxTravelDistanceKm.Value;
            }

            // Check for conflicts (excluding current availability)
            var hasConflict = await _availabilityRepository.HasConflictAsync(
                availability.TutorId,
                availability.DayOfWeek,
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
            if (availability.CurrentBookings > 0)
            {
                throw new Exception($"Cannot delete availability with active bookings ({availability.CurrentBookings})");
            }

            await _availabilityRepository.DeleteAsync(availabilityId);
        }

        public async Task<TutorAvailabilityResponse> GetAvailabilityByIdAsync(Guid availabilityId)
        {
            var availability = await _availabilityRepository.GetByIdAsync(availabilityId);
            if (availability == null)
            {
                return null;
            }

            return MapToResponse(availability);
        }

        public async Task<List<TutorAvailabilityResponse>> GetTutorAvailabilitiesAsync(Guid tutorId, bool activeOnly = true)
        {
            List<TutorAvailability> availabilities;

            if (activeOnly)
            {
                availabilities = await _availabilityRepository.GetActiveTutorAvailabilitiesAsync(tutorId);
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
            if (request.DayOfWeek < 0 || request.DayOfWeek > 6)
            {
                throw new ArgumentException("Day of week must be between 0 (Sunday) and 6 (Saturday)");
            }

            if (request.EndTime <= request.StartTime)
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
                request.DayOfWeek,
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
                    TotalAvailableSlots = group.Sum(a => a.MaxConcurrentBookings - a.CurrentBookings),
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

        public async Task<bool> CheckAvailabilityConflictAsync(
            Guid tutorId,
            int dayOfWeek,
            TimeOnly startTime,
            TimeOnly endTime,
            DateOnly effectiveFrom,
            DateOnly? effectiveUntil,
            Guid? excludeAvailabilityId = null)
        {
            return await _availabilityRepository.HasConflictAsync(
                tutorId,
                dayOfWeek,
                startTime,
                endTime,
                effectiveFrom,
                effectiveUntil,
                excludeAvailabilityId);
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

        public async Task IncrementBookingCountAsync(Guid availabilityId)
        {
            await _availabilityRepository.UpdateBookingCountAsync(availabilityId, 1);
        }

        public async Task DecrementBookingCountAsync(Guid availabilityId)
        {
            await _availabilityRepository.UpdateBookingCountAsync(availabilityId, -1);
        }

        public async Task<List<Guid>> BulkCreateAvailabilitiesAsync(List<CreateTutorAvailabilityRequest> requests)
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
                    throw new Exception($"Failed to create availability for day {request.DayOfWeek}: {ex.Message}");
                }
            }

            return createdIds;
        }

        private TutorAvailabilityResponse MapToResponse(TutorAvailability availability)
        {
            return new TutorAvailabilityResponse
            {
                AvailabilityId = availability.AvailabilityId,
                TutorId = availability.TutorId,
                TutorName = availability.Tutor?.FullName ?? "Unknown",
                DayOfWeek = availability.DayOfWeek,
                DayOfWeekName = GetDayOfWeekName(availability.DayOfWeek),
                AvailableFrom = availability.AvailableFrom,
                AvailableUntil = availability.AvailableUntil,
                EffectiveFrom = availability.EffectiveFrom,
                EffectiveUntil = availability.EffectiveUntil,
                MaxConcurrentBookings = availability.MaxConcurrentBookings,
                CurrentBookings = availability.CurrentBookings,
                AvailableSlots = availability.MaxConcurrentBookings - availability.CurrentBookings,
                CanTeachOnline = availability.CanTeachOnline,
                CanTeachOffline = availability.CanTeachOffline,
                MaxTravelDistanceKm = availability.MaxTravelDistanceKm,
                Status = availability.Status,
                CreatedDate = availability.CreatedDate,
                UpdatedDate = availability.UpdatedDate
            };
        }

        private string GetDayOfWeekName(int dayOfWeek)
        {
            return dayOfWeek switch
            {
                0 => "Sunday",
                1 => "Monday",
                2 => "Tuesday",
                3 => "Wednesday",
                4 => "Thursday",
                5 => "Friday",
                6 => "Saturday",
                _ => "Unknown"
            };
        }
    }
}