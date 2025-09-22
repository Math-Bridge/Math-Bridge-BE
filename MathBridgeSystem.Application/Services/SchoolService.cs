using MathBridge.Application.DTOs;
using MathBridge.Application.Interfaces;
using MathBridge.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MathBridgeSystem.Application.Interfaces;
using MathBridge.Domain.Interfaces;
using MathBridgeSystem.Application.DTOs;

namespace MathBridge.Application.Services
{
    public class SchoolService : ISchoolService
    {
        private readonly ISchoolRepository _schoolRepository;
        private readonly IGoogleMapsService _googleMapsService;

        public SchoolService(ISchoolRepository schoolRepository, IGoogleMapsService googleMapsService)
        {
            _schoolRepository = schoolRepository ?? throw new ArgumentNullException(nameof(schoolRepository));
            _googleMapsService = googleMapsService ?? throw new ArgumentNullException(nameof(googleMapsService));
        }

        public async Task<School> CreateSchoolAsync(CreateSchoolRequest request)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            // Fetch place details
            var placeDetailsResponse = await _googleMapsService.GetPlaceDetailsAsync(request.PlaceId);
            if (!placeDetailsResponse.Success || placeDetailsResponse.Place == null)
                throw new Exception("Failed to fetch place details from Google Maps");

            var place = placeDetailsResponse.Place;

            var school = new School
            {
                SchoolId = Guid.NewGuid(),
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

            await _schoolRepository.AddAsync(school);
            return school;
        }

        public async Task<SchoolResponse> GetSchoolByIdAsync(Guid id)
        {
            var school = await _schoolRepository.GetByIdAsync(id);
            if (school == null)
                throw new Exception("School not found");

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

        public async Task<IEnumerable<SchoolResponse>> GetAllSchoolsAsync()
        {
            var schools = await _schoolRepository.GetAllAsync();
            return schools.Select(s => new SchoolResponse
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

        public async Task<School> UpdateSchoolAsync(Guid id, UpdateSchoolRequest request)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            var school = await _schoolRepository.GetByIdAsync(id);
            if (school == null)
                throw new Exception("School not found");

            if (!string.IsNullOrEmpty(request.Name))
                school.Name = request.Name;

            if (!string.IsNullOrEmpty(request.PlaceId))
            {
                var placeDetailsResponse = await _googleMapsService.GetPlaceDetailsAsync(request.PlaceId);
                if (!placeDetailsResponse.Success || placeDetailsResponse.Place == null)
                    throw new Exception("Failed to fetch place details from Google Maps");

                var place = placeDetailsResponse.Place;
                school.GooglePlaceId = request.PlaceId;
                school.FormattedAddress = place.FormattedAddress;
                school.UpdatedDate = DateTime.UtcNow;
                school.Latitude = place.Latitude;
                school.Longitude = place.Longitude;
                school.City = place.City;
                school.District = place.District;
                school.PlaceName = place.PlaceName;
                school.CountryCode = place.CountryCode;
                school.LocationUpdatedDate = DateTime.UtcNow;
            }

            school.UpdatedDate = DateTime.UtcNow;

            await _schoolRepository.UpdateAsync(school);
            return school;
        }

        public async Task DeleteSchoolAsync(Guid id)
        {
            if (!await _schoolRepository.ExistsAsync(id))
                throw new Exception("School not found");

            await _schoolRepository.DeleteAsync(id);
        }
    }
}