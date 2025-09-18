using MathBridgeSystem.Application.DTOs;
using MathBridgeSystem.Application.Interfaces;
using MathBridge.Domain.Entities;
using MathBridgeSystem.Domain.Interfaces;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using MathBridge.Application.Interfaces;
using MathBridge.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace MathBridgeSystem.Application.Services
{
    public class SchoolService : ISchoolService
    {
        private readonly ISchoolRepository _schoolRepository;
        private readonly IGoogleMapsService _googleMapsService;
        private readonly ILogger<SchoolService> _logger;
        private readonly IUserRepository _userRepository;

        public SchoolService(ISchoolRepository schoolRepository, IGoogleMapsService googleMapsService, IUserRepository userRepository, ILogger<SchoolService> logger)
        {
            _schoolRepository = schoolRepository ?? throw new ArgumentNullException(nameof(schoolRepository));
            _googleMapsService = googleMapsService ?? throw new ArgumentNullException(nameof(googleMapsService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
        }

        public async Task<School> CreateSchoolAsync(CreateSchoolRequest request)
        {
            try
            {
                _logger.LogInformation("Creating school with Name: '{Name}' and PlaceId: '{PlaceId}'", 
                    request?.Name ?? "NULL", request?.PlaceId ?? "NULL");

                if (request == null)
                {
                    _logger.LogWarning("CreateSchoolRequest is null");
                    throw new ArgumentNullException(nameof(request));
                }

                if (string.IsNullOrEmpty(request.PlaceId))
                {
                    _logger.LogWarning("PlaceId is null or empty for school creation");
                    throw new ArgumentException("PlaceId is required", nameof(request));
                }

                // Fetch place details from Google Maps
                _logger.LogInformation("Fetching place details from Google Maps for PlaceId: '{PlaceId}'", request.PlaceId);
                var placeDetailsResponse = await _googleMapsService.GetPlaceDetailsAsync(request.PlaceId);
                
                if (!placeDetailsResponse.Success || placeDetailsResponse.Place == null)
                {
                    _logger.LogWarning("Failed to fetch place details from Google Maps for PlaceId: '{PlaceId}'. " +
                        "Success: {Success}, Place: {Place}", 
                        request.PlaceId, placeDetailsResponse.Success, placeDetailsResponse.Place != null);
                    throw new InvalidOperationException($"Failed to fetch place details from Google Maps for PlaceId: {request.PlaceId}");
                }

                var place = placeDetailsResponse.Place;
                _logger.LogInformation("Successfully fetched place details for '{PlaceName}' at '{FormattedAddress}'", 
                    place.PlaceName, place.FormattedAddress);

                var schoolId = Guid.NewGuid();
                var school = new School
                {
                    SchoolId = schoolId,
                    Name = request.Name,
                    GooglePlaceId = request.PlaceId,
                    FormattedAddress = place.FormattedAddress,
                    UpdatedDate = DateTime.UtcNow,
                    Latitude = place.Latitude,
                    Longitude = place.Longitude,
                    City = place.City,
                    District = place.District,
                    PlaceName = place.PlaceName,
                    CountryCode = place.CountryCode,
                    CreatedDate = DateTime.UtcNow,
                    LocationUpdatedDate = DateTime.UtcNow
                };

                _logger.LogInformation("Adding school to repository with ID: {SchoolId}", schoolId);
                await _schoolRepository.AddAsync(school);
                
                _logger.LogInformation("Successfully created school '{Name}' with ID: {SchoolId}", 
                    school.Name, school.SchoolId);
                return school;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating school with Name: '{Name}' and PlaceId: '{PlaceId}'", 
                    request?.Name ?? "NULL", request?.PlaceId ?? "NULL");
                throw;
            }
        }

        public async Task<SchoolResponse> GetSchoolByIdAsync(Guid id)
        {
            try
            {
                _logger.LogInformation("Retrieving school with ID: {SchoolId}", id);

                var school = await _schoolRepository.GetByIdAsync(id);
                if (school == null)
                {
                    _logger.LogWarning("School not found with ID: {SchoolId}", id);
                    throw new KeyNotFoundException($"School with ID {id} not found");
                }

                _logger.LogInformation("Successfully retrieved school '{Name}' with ID: {SchoolId}", 
                    school.Name, school.SchoolId);

                return new SchoolResponse
                {
                    SchoolId = school.SchoolId,
                    Name = school.Name,
                    GooglePlaceId = school.GooglePlaceId,
                    FormattedAddress = school.FormattedAddress,
                    Latitude = school.Latitude,
                    Longitude = school.Longitude,
                    City = school.City,
                    District = school.District,
                    PlaceName = school.PlaceName,
                    CountryCode = school.CountryCode,
                    CreatedDate = school.CreatedDate,
                    UpdatedDate = school.UpdatedDate,
                    LocationUpdatedDate = school.LocationUpdatedDate
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving school with ID: {SchoolId}", id);
                throw;
            }
        }

        public async Task<IEnumerable<SchoolResponse>> GetAllSchoolsAsync()
        {
            try
            {
                _logger.LogInformation("Retrieving all schools from repository");

                var schools = await _schoolRepository.GetAllAsync();
                var schoolsList = schools.ToList();
                
                _logger.LogInformation("Successfully retrieved {Count} schools", schoolsList.Count);

                return schoolsList.Select(s => new SchoolResponse
                {
                    SchoolId = s.SchoolId,
                    Name = s.Name,
                    GooglePlaceId = s.GooglePlaceId,
                    FormattedAddress = s.FormattedAddress,
                    Latitude = s.Latitude,
                    Longitude = s.Longitude,
                    City = s.City,
                    District = s.District,
                    PlaceName = s.PlaceName,
                    CountryCode = s.CountryCode,
                    CreatedDate = s.CreatedDate,
                    UpdatedDate = s.UpdatedDate,
                    LocationUpdatedDate = s.LocationUpdatedDate
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving all schools");
                throw;
            }
        }

        public async Task<School> UpdateSchoolAsync(Guid id, UpdateSchoolRequest request)
        {
            try
            {
                _logger.LogInformation("Updating school with ID: {SchoolId}, Name: '{Name}', PlaceId: '{PlaceId}'", 
                    id, request?.Name ?? "NULL", request?.PlaceId ?? "NULL");

                if (request == null)
                {
                    _logger.LogWarning("UpdateSchoolRequest is null for school ID: {SchoolId}", id);
                    throw new ArgumentNullException(nameof(request));
                }

                var school = await _schoolRepository.GetByIdAsync(id);
                if (school == null)
                {
                    _logger.LogWarning("School not found with ID: {SchoolId} for update", id);
                    throw new KeyNotFoundException($"School with ID {id} not found");
                }

                _logger.LogInformation("Found existing school '{Name}' with ID: {SchoolId}", school.Name, school.SchoolId);

                // Update name if provided
                if (!string.IsNullOrEmpty(request.Name))
                {
                    _logger.LogInformation("Updating school name from '{OldName}' to '{NewName}'", 
                        school.Name, request.Name);
                    school.Name = request.Name;
                }

                // Update place information if PlaceId is provided
                if (!string.IsNullOrEmpty(request.PlaceId))
                {
                    _logger.LogInformation("Updating school location from PlaceId '{OldPlaceId}' to '{NewPlaceId}'", 
                        school.GooglePlaceId, request.PlaceId);

                    var placeDetailsResponse = await _googleMapsService.GetPlaceDetailsAsync(request.PlaceId);
                    if (!placeDetailsResponse.Success || placeDetailsResponse.Place == null)
                    {
                        _logger.LogWarning("Failed to fetch place details from Google Maps for PlaceId: '{PlaceId}' during school update. " +
                            "Success: {Success}, Place: {Place}", 
                            request.PlaceId, placeDetailsResponse.Success, placeDetailsResponse.Place != null);
                        throw new InvalidOperationException($"Failed to fetch place details from Google Maps for PlaceId: {request.PlaceId}");
                    }

                    var place = placeDetailsResponse.Place;
                    _logger.LogInformation("Successfully fetched new place details for '{PlaceName}' at '{FormattedAddress}'", 
                        place.PlaceName, place.FormattedAddress);

                    school.GooglePlaceId = request.PlaceId;
                    school.FormattedAddress = place.FormattedAddress;
                    school.Latitude = place.Latitude;
                    school.Longitude = place.Longitude;
                    school.City = place.City;
                    school.District = place.District;
                    school.PlaceName = place.PlaceName;
                    school.CountryCode = place.CountryCode;
                    school.LocationUpdatedDate = DateTime.UtcNow;
                }

                school.UpdatedDate = DateTime.UtcNow;

                _logger.LogInformation("Updating school in repository with ID: {SchoolId}", school.SchoolId);
                await _schoolRepository.UpdateAsync(school);

                _logger.LogInformation("Successfully updated school '{Name}' with ID: {SchoolId}", 
                    school.Name, school.SchoolId);
                return school;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating school with ID: {SchoolId}", id);
                throw;
            }
        }

        public async Task DeleteSchoolAsync(Guid id)
        {
            try
            {
                _logger.LogInformation("Deleting school with ID: {SchoolId}", id);

                var exists = await _schoolRepository.ExistsAsync(id);
                if (!exists)
                {
                    _logger.LogWarning("School not found with ID: {SchoolId} for deletion", id);
                    throw new KeyNotFoundException($"School with ID {id} not found");
                }

                _logger.LogInformation("School exists, proceeding with deletion for ID: {SchoolId}", id);
                await _schoolRepository.DeleteAsync(id);
                
                _logger.LogInformation("Successfully deleted school with ID: {SchoolId}", id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting school with ID: {SchoolId}", id);
                throw;
            }
        }

private static double ToRadians(double angle)
        {
            return angle * Math.PI / 180.0;
        }

        private static double CalculateDistance(double lat1, double lon1, double lat2, double lon2)
        {
            const double earthRadius = 6371.0; // Earth's radius in km

            var dLat = ToRadians(lat2 - lat1);
            var dLon = ToRadians(lon2 - lon1);

            var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                    Math.Cos(ToRadians(lat1)) * Math.Cos(ToRadians(lat2)) *
                    Math.Sin(dLon / 2) * Math.Sin(dLon / 2);

            var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));

            return earthRadius * c;
        }

        /// <summary>
        /// Searches for schools within a specified radius (in km) from the current user's location.
        /// </summary>
        /// <param name="userId">The ID of the current user.</param>
        /// <param name="radiusKm">The search radius in kilometers.</param>
        /// <returns>A list of schools within the radius.</returns>
        public async Task<IEnumerable<SchoolResponse>> SearchByRadiusAsync(Guid userId, double radiusKm)
        {
            try
            {
                _logger.LogInformation("Searching schools within {RadiusKm} km of user {UserId}", radiusKm, userId);

                if (radiusKm <= 0)
                {
                    _logger.LogWarning("Invalid radius {RadiusKm} provided for search", radiusKm);
                    throw new ArgumentException("Radius must be greater than 0", nameof(radiusKm));
                }

                var user = await _userRepository.GetByIdAsync(userId);
                if (user == null || user.Latitude == null || user.Longitude == null)
                {
                    _logger.LogWarning("User {UserId} not found or has no location", userId);
                    return Enumerable.Empty<SchoolResponse>();
                }

                _logger.LogInformation("User {UserId} location: {Latitude}, {Longitude}", userId, user.Latitude, user.Longitude);

                var schools = await _schoolRepository.GetAllAsync();
                var schoolsList = schools.ToList();

                _logger.LogInformation("Loaded {Count} schools for distance calculation", schoolsList.Count);

                var filteredSchools = schoolsList
                    .Where(s => s.Latitude != null && s.Longitude != null)
                    .Select(s => new
                    {
                        School = s,
                        Distance = CalculateDistance(user.Latitude.Value, user.Longitude.Value, s.Latitude.Value, s.Longitude.Value)
                    })
                    .Where(x => x.Distance <= radiusKm)
                    .OrderBy(x => x.Distance)
                    .Select(x => new SchoolResponse
                    {
                        SchoolId = x.School.SchoolId,
                        Name = x.School.Name,
                        GooglePlaceId = x.School.GooglePlaceId,
                        FormattedAddress = x.School.FormattedAddress,
                        Latitude = x.School.Latitude,
                        Longitude = x.School.Longitude,
                        City = x.School.City,
                        District = x.School.District,
                        PlaceName = x.School.PlaceName,
                        CountryCode = x.School.CountryCode,
                        CreatedDate = x.School.CreatedDate,
                        UpdatedDate = x.School.UpdatedDate,
                        LocationUpdatedDate = x.School.LocationUpdatedDate
                    });

                _logger.LogInformation("Found {Count} schools within {RadiusKm} km of user {UserId}", filteredSchools.Count(), radiusKm, userId);

                return filteredSchools;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching schools for user {UserId} with radius {RadiusKm}", userId, radiusKm);
                throw;
            }
        }
    }
}
