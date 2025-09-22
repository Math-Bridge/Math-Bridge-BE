using MathBridge.Application.DTOs;
using MathBridge.Application.Interfaces;
using MathBridge.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MathBridgeSystem.Application.Interfaces;
using MathBridge.Domain.Interfaces;
using MathBridgeSystem.Application.DTOs;
using MathBridge.Infrastructure.Data;
using MathBridgeSystem.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace MathBridge.Application.Services
{
    public class CenterService : ICenterService
    {
        private readonly ICenterRepository _centerRepository;
        private readonly ITutorCenterRepository _tutorCenterRepository;
        private readonly IUserRepository _userRepository;
        private readonly MathBridgeDbContext _context;
        private readonly IGoogleMapsService _googleMapsService;

        public CenterService(
            ICenterRepository centerRepository,
            ITutorCenterRepository tutorCenterRepository,
            IUserRepository userRepository,
            MathBridgeDbContext context,
            IGoogleMapsService googleMapsService)
        {
            _centerRepository = centerRepository ?? throw new ArgumentNullException(nameof(centerRepository));
            _tutorCenterRepository = tutorCenterRepository ?? throw new ArgumentNullException(nameof(tutorCenterRepository));
            _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _googleMapsService = googleMapsService ?? throw new ArgumentNullException(nameof(googleMapsService));
        }

        public async Task<Guid> CreateCenterAsync(CreateCenterRequest request)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            if (string.IsNullOrWhiteSpace(request.Name))
                throw new ArgumentException("Center name is required", nameof(request.Name));

            if (string.IsNullOrWhiteSpace(request.PlaceId))
                throw new ArgumentException("PlaceId is required", nameof(request.PlaceId));

            // Fetch place details from Google Maps
            var placeDetailsResponse = await _googleMapsService.GetPlaceDetailsAsync(request.PlaceId);
            if (!placeDetailsResponse.Success || placeDetailsResponse.Place == null)
                throw new Exception("Failed to fetch place details from Google Maps");

            var place = placeDetailsResponse.Place;

            // Check if center already exists by name and location
            var existingCenters = await _centerRepository.GetAllAsync();
            var existingCenter = existingCenters
                .FirstOrDefault(c => c.Name == request.Name &&
                                   c.City == place.City &&
                                   c.District == place.District);

            if (existingCenter != null)
                throw new Exception($"Center with name '{request.Name}' at location {place.City}, {place.District} already exists");

            var center = new Center
            {
                CenterId = Guid.NewGuid(),
                Name = request.Name,
                GooglePlaceId = request.PlaceId,
                FormattedAddress = place.FormattedAddress,
                Latitude = place.Latitude,
                Longitude = place.Longitude,
                City = place.City,
                District = place.District,
                PlaceName = place.PlaceName,
                CountryCode = place.CountryCode ?? "VN",
                CreatedDate = DateTime.UtcNow,
                UpdatedDate = DateTime.UtcNow,
                LocationUpdatedDate = DateTime.UtcNow,
                TutorCount = 0
            };

            await _centerRepository.AddAsync(center);
            return center.CenterId;
        }

        public async Task UpdateCenterAsync(Guid id, UpdateCenterRequest request)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            var center = await _centerRepository.GetByIdAsync(id);
            if (center == null)
                throw new Exception("Center not found");

            bool hasChanges = false;

            // Update name if provided
            if (!string.IsNullOrWhiteSpace(request.Name))
            {
                if (request.Name != center.Name)
                {
                    // Check if name and location combination already exists (excluding current center)
                    var existingCenters = await _centerRepository.GetAllAsync();
                    var existingCenter = existingCenters
                        .FirstOrDefault(c => c.CenterId != id &&
                                           c.Name == request.Name &&
                                           c.City == center.City &&
                                           c.District == center.District);

                    if (existingCenter != null)
                        throw new Exception($"Another center with name '{request.Name}' at location {center.City}, {center.District} already exists");

                    center.Name = request.Name;
                    hasChanges = true;
                }
            }

            // Update location if PlaceId is provided
            if (!string.IsNullOrWhiteSpace(request.PlaceId))
            {
                if (request.PlaceId != center.GooglePlaceId)
                {
                    var placeDetailsResponse = await _googleMapsService.GetPlaceDetailsAsync(request.PlaceId);
                    if (!placeDetailsResponse.Success || placeDetailsResponse.Place == null)
                        throw new Exception("Failed to fetch place details from Google Maps");

                    var place = placeDetailsResponse.Place;

                    // Check if name and new location combination already exists (excluding current center)
                    var nameToCheck = !string.IsNullOrWhiteSpace(request.Name) ? request.Name : center.Name;
                    var existingCenters = await _centerRepository.GetAllAsync();
                    var existingCenter = existingCenters
                        .FirstOrDefault(c => c.CenterId != id &&
                                           c.Name == nameToCheck &&
                                           c.City == place.City &&
                                           c.District == place.District);

                    if (existingCenter != null)
                        throw new Exception($"Another center with name '{nameToCheck}' at location {place.City}, {place.District} already exists");

                    center.GooglePlaceId = request.PlaceId;
                    center.FormattedAddress = place.FormattedAddress;
                    center.Latitude = place.Latitude;
                    center.Longitude = place.Longitude;
                    center.City = place.City;
                    center.District = place.District;
                    center.PlaceName = place.PlaceName;
                    center.CountryCode = place.CountryCode ?? "VN";
                    center.LocationUpdatedDate = DateTime.UtcNow;
                    hasChanges = true;
                }
            }

            if (hasChanges)
            {
                center.UpdatedDate = DateTime.UtcNow;
                await _centerRepository.UpdateAsync(center);
            }
        }

        public async Task DeleteCenterAsync(Guid id)
        {
            var center = await _centerRepository.GetByIdAsync(id);
            if (center == null)
                throw new Exception("Center not found");

            // Check if center has active contracts
            var activeContracts = await _context.Contracts
                .AnyAsync(c => c.CenterId == id && c.Status == "active");

            if (activeContracts)
                throw new Exception("Cannot delete center with active contracts");

            // Check if center has assigned children
            var assignedChildren = await _context.Children
                .AnyAsync(c => c.CenterId == id && c.Status == "active");

            if (assignedChildren)
                throw new Exception("Cannot delete center with assigned children");

            // Remove all tutor assignments
            var tutorCenters = await _tutorCenterRepository.GetByCenterIdAsync(id);
            foreach (var tc in tutorCenters)
            {
                await _tutorCenterRepository.RemoveAsync(tc.TutorCenterId);
            }

            await _centerRepository.DeleteAsync(id);
        }

        public async Task<CenterDto> GetCenterByIdAsync(Guid id)
        {
            var center = await _centerRepository.GetByIdAsync(id);
            if (center == null)
                throw new Exception("Center not found");

            return MapToCenterDto(center);
        }

        public async Task<List<CenterDto>> GetAllCentersAsync()
        {
            var centers = await _centerRepository.GetAllAsync();
            return centers.Select(MapToCenterDto).ToList();
        }

        public async Task<List<CenterWithTutorsDto>> GetCentersWithTutorsAsync()
        {
            var centers = await _centerRepository.GetAllAsync();
            var result = new List<CenterWithTutorsDto>();

            foreach (var center in centers)
            {
                // Get tutors assigned to this center
                var tutors = await _userRepository.GetTutorsByCenterAsync(center.CenterId);

                var centerWithTutors = new CenterWithTutorsDto
                {
                    CenterId = center.CenterId,
                    Name = center.Name,
                    Latitude = center.Latitude,
                    Longitude = center.Longitude,
                    FormattedAddress = center.FormattedAddress,
                    City = center.City,
                    District = center.District,
                    PlaceName = center.PlaceName,
                    CountryCode = center.CountryCode,
                    TutorCount = tutors.Count,
                    CreatedDate = center.CreatedDate,
                    UpdatedDate = center.UpdatedDate,
                    Tutors = tutors.Select(t => new TutorInCenterDto
                    {
                        TutorId = t.UserId,
                        FullName = t.FullName,
                        Email = t.Email,
                        PhoneNumber = t.PhoneNumber,
                        HourlyRate = t.TutorVerification?.HourlyRate ?? 0m,
                        Bio = t.TutorVerification?.Bio,
                        VerificationStatus = t.TutorVerification?.VerificationStatus ?? "pending",
                        CreatedDate = t.CreatedDate
                    }).ToList()
                };
                result.Add(centerWithTutors);
            }

            return result;
        }

        public async Task<List<CenterDto>> SearchCentersAsync(CenterSearchRequest request)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            var centers = await _centerRepository.GetAllAsync();
            var query = centers.AsQueryable();

            // Apply filters
            if (!string.IsNullOrEmpty(request.City))
                query = query.Where(c => !string.IsNullOrEmpty(c.City) && c.City.Contains(request.City, StringComparison.OrdinalIgnoreCase));

            if (!string.IsNullOrEmpty(request.District))
                query = query.Where(c => !string.IsNullOrEmpty(c.District) && c.District.Contains(request.District, StringComparison.OrdinalIgnoreCase));

            if (!string.IsNullOrEmpty(request.Name))
                query = query.Where(c => c.Name.Contains(request.Name, StringComparison.OrdinalIgnoreCase));

            // For location-based search, check if coordinates are provided
            if (request.Latitude.HasValue && request.Longitude.HasValue && request.RadiusKm.HasValue)
            {
                query = query.Where(c => c.Latitude.HasValue && c.Longitude.HasValue &&
                    CalculateDistance(request.Latitude.Value, request.Longitude.Value,
                                    c.Latitude.Value, c.Longitude.Value) <= request.RadiusKm.Value);
            }

            // Apply pagination
            var pageIndex = Math.Max(1, request.Page);
            var pageSize = Math.Max(1, request.PageSize);

            var filteredCenters = query
                .OrderBy(c => c.City ?? "")
                .ThenBy(c => c.Name)
                .Skip((pageIndex - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            return filteredCenters.Select(MapToCenterDto).ToList();
        }

        public async Task<int> GetCentersCountByCriteriaAsync(CenterSearchRequest request)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            var centers = await _centerRepository.GetAllAsync();
            var query = centers.AsQueryable();

            // Apply same filters as SearchCentersAsync
            if (!string.IsNullOrEmpty(request.City))
                query = query.Where(c => !string.IsNullOrEmpty(c.City) && c.City.Contains(request.City, StringComparison.OrdinalIgnoreCase));

            if (!string.IsNullOrEmpty(request.District))
                query = query.Where(c => !string.IsNullOrEmpty(c.District) && c.District.Contains(request.District, StringComparison.OrdinalIgnoreCase));

            if (!string.IsNullOrEmpty(request.Name))
                query = query.Where(c => c.Name.Contains(request.Name, StringComparison.OrdinalIgnoreCase));

            if (request.Latitude.HasValue && request.Longitude.HasValue && request.RadiusKm.HasValue)
            {
                query = query.Where(c => c.Latitude.HasValue && c.Longitude.HasValue &&
                    CalculateDistance(request.Latitude.Value, request.Longitude.Value,
                                    c.Latitude.Value, c.Longitude.Value) <= request.RadiusKm.Value);
            }

            return query.Count();
        }

        public async Task<List<CenterDto>> GetCentersByCityAsync(string city)
        {
            if (string.IsNullOrWhiteSpace(city))
                return new List<CenterDto>();

            var centers = await _centerRepository.GetByCityAsync(city);
            return centers.Select(MapToCenterDto).ToList();
        }

        public async Task<List<CenterDto>> GetCentersNearLocationAsync(double latitude, double longitude, double radiusKm = 10.0)
        {
            if (radiusKm < 0)
                throw new ArgumentException("Radius cannot be negative", nameof(radiusKm));

            var centers = await _centerRepository.GetByCoordinates(latitude, longitude, radiusKm);
            return centers.Select(MapToCenterDto).ToList();
        }

        public async Task AssignTutorToCenterAsync(Guid centerId, Guid tutorId)
        {
            var center = await _centerRepository.GetByIdAsync(centerId);
            if (center == null)
                throw new Exception("Center not found");

            // Validate tutor exists and is verified
            var tutor = await _userRepository.GetTutorWithVerificationAsync(tutorId);
            if (tutor == null)
                throw new Exception("Tutor not found");

            if (tutor.TutorVerification == null || tutor.TutorVerification.VerificationStatus != "approved")
                throw new Exception("Tutor must be verified to be assigned to a center");

            // Check if tutor is already assigned to this center
            var alreadyAssigned = await _tutorCenterRepository.TutorIsAssignedToCenterAsync(tutorId, centerId);
            if (alreadyAssigned)
                throw new Exception("Tutor is already assigned to this center");

            var tutorCenter = new TutorCenter
            {
                TutorCenterId = Guid.NewGuid(),
                TutorId = tutorId,
                CenterId = centerId,
                CreatedDate = DateTime.UtcNow
            };

            await _tutorCenterRepository.AddAsync(tutorCenter);

            // Update tutor count
            await _centerRepository.UpdateTutorCountAsync(centerId, 1);
        }

        public async Task RemoveTutorFromCenterAsync(Guid centerId, Guid tutorId)
        {
            var center = await _centerRepository.GetByIdAsync(centerId);
            if (center == null)
                throw new Exception("Center not found");

            // Validate tutor exists
            var tutor = await _userRepository.GetTutorWithVerificationAsync(tutorId);
            if (tutor == null)
                throw new Exception("Tutor not found");

            // Check if tutor is assigned to this center
            var isAssigned = await _tutorCenterRepository.TutorIsAssignedToCenterAsync(tutorId, centerId);
            if (!isAssigned)
                throw new Exception("Tutor is not assigned to this center");

            // Find and remove the assignment
            var tutorCenters = await _tutorCenterRepository.GetByTutorIdAsync(tutorId);
            var assignment = tutorCenters.FirstOrDefault(tc => tc.CenterId == centerId);

            if (assignment != null)
            {
                await _tutorCenterRepository.RemoveAsync(assignment.TutorCenterId);

                // Update tutor count
                await _centerRepository.UpdateTutorCountAsync(centerId, -1);
            }
        }

        public async Task<List<TutorInCenterDto>> GetTutorsByCenterIdAsync(Guid centerId)
        {
            var center = await _centerRepository.GetByIdAsync(centerId);
            if (center == null)
                throw new Exception("Center not found");

            var tutors = await _userRepository.GetTutorsByCenterAsync(centerId);

            return tutors.Select(t => new TutorInCenterDto
            {
                TutorId = t.UserId,
                FullName = t.FullName,
                Email = t.Email,
                PhoneNumber = t.PhoneNumber,
                HourlyRate = t.TutorVerification?.HourlyRate ?? 0m,
                Bio = t.TutorVerification?.Bio,
                VerificationStatus = t.TutorVerification?.VerificationStatus ?? "pending",
                CreatedDate = t.CreatedDate
            }).ToList();
        }

        private CenterDto MapToCenterDto(Center center)
        {
            if (center == null)
                return null;

            return new CenterDto
            {
                CenterId = center.CenterId,
                Name = center.Name ?? string.Empty,
                Latitude = center.Latitude,
                Longitude = center.Longitude,
                FormattedAddress = center.FormattedAddress,
                City = center.City,
                District = center.District,
                PlaceName = center.PlaceName,
                CountryCode = center.CountryCode,
                TutorCount = center.TutorCount,
                CreatedDate = center.CreatedDate,
                UpdatedDate = center.UpdatedDate
            };
        }

        private double CalculateDistance(double? lat1, double? lon1, double? lat2, double? lon2)
        {
            if (!lat1.HasValue || !lon1.HasValue || !lat2.HasValue || !lon2.HasValue)
                return double.MaxValue; // Return very large distance if any coordinate is null

            // Haversine formula for distance calculation
            const double R = 6371; // Earth's radius in km
            var dLat = ToRadians(lat2.Value - lat1.Value);
            var dLon = ToRadians(lon2.Value - lon1.Value);
            var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                    Math.Cos(ToRadians(lat1.Value)) * Math.Cos(ToRadians(lat2.Value)) *
                    Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
            var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
            return R * c;
        }

        private double ToRadians(double degrees)
        {
            return degrees * Math.PI / 180;
        }
    }
}