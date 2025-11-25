using MathBridgeSystem.Application.DTOs;
using MathBridgeSystem.Application.Interfaces;
using MathBridgeSystem.Domain.Entities;
using MathBridgeSystem.Domain.Interfaces;
using MathBridgeSystem.Infrastructure.Data;
using MathBridgeSystem.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace MathBridgeSystem.Application.Services
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
            if (request == null) throw new ArgumentNullException(nameof(request));
            if (string.IsNullOrWhiteSpace(request.Name))
                throw new ArgumentException("Center name is required.", nameof(request.Name));
            if (string.IsNullOrWhiteSpace(request.PlaceId))
                throw new ArgumentException("Google PlaceId is required.", nameof(request.PlaceId));

            var placeResponse = await _googleMapsService.GetPlaceDetailsAsync(request.PlaceId);
            if (!placeResponse.Success || placeResponse.Place == null)
                throw new Exception("Failed to retrieve place details from Google Maps. Invalid PlaceId.");

            dynamic place = placeResponse.Place; // Dùng dynamic để hỗ trợ mọi tên property

            // Lấy dữ liệu an toàn 100% dù tên property là gì
            string formattedAddress = GetString(place, "FormattedAddress", "Address", "Vicinity", "formatted_address");
            string city = GetString(place, "City", "AdminAreaLevel1", "administrative_area_level_1");
            string district = GetString(place, "District", "AdminAreaLevel2", "administrative_area_level_2");
            string placeName = GetString(place, "Name", "PlaceName", "name");
            double latitude = GetDouble(place, "Lat", "Latitude", "lat", "latitude");
            double longitude = GetDouble(place, "Lng", "Longitude", "lng", "longitude");

            if (string.IsNullOrWhiteSpace(formattedAddress))
                throw new Exception("Could not retrieve valid address from Google Maps.");

            var newName = request.Name.Trim();
            var existingCenters = await _centerRepository.GetAllAsync();

            foreach (var center in existingCenters)
            {
                // 1. Cùng Google PlaceId
                if (center.GooglePlaceId == request.PlaceId)
                    throw new Exception($"A center already exists at this exact Google location: \"{center.Name}\" – {center.FormattedAddress}");

                // 2. Cùng tên + cùng khu vực
                if (string.Equals(center.Name, newName, StringComparison.OrdinalIgnoreCase) &&
                    string.Equals(center.City, city, StringComparison.OrdinalIgnoreCase) &&
                    string.Equals(center.District, district, StringComparison.OrdinalIgnoreCase))
                    throw new Exception($"A center named \"{newName}\" already exists in {city}, {district}.");

                // 3. Địa chỉ quá giống (chuỗi)
                if (!string.IsNullOrWhiteSpace(center.FormattedAddress) &&
                    AreAddressesTooSimilar(center.FormattedAddress, formattedAddress))
                    throw new Exception($"The address is too similar to an existing center: \"{center.Name}\" – {center.FormattedAddress}");

                // 4. Quá gần về tọa độ (< 150m)
                if (center.Latitude.HasValue && center.Longitude.HasValue && latitude != 0 && longitude != 0)
                {
                    var distance = CalculateDistance(latitude, longitude, center.Latitude.Value, center.Longitude.Value);
                    if (distance <= 0.15)
                    {
                        var meters = Math.Round(distance * 1000);
                        throw new Exception($"Location is too close (~{meters}m) to existing center: \"{center.Name}\"");
                    }
                }
            }

            var newCenter = new Center
            {
                CenterId = Guid.NewGuid(),
                Name = newName,
                GooglePlaceId = request.PlaceId,
                FormattedAddress = formattedAddress,
                Latitude = latitude == 0 ? null : latitude,
                Longitude = longitude == 0 ? null : longitude,
                City = city,
                District = district,
                PlaceName = placeName,
                CountryCode = "VN",
                CreatedDate = DateTime.UtcNow.ToLocalTime(),
                UpdatedDate = DateTime.UtcNow.ToLocalTime(),
                LocationUpdatedDate = DateTime.UtcNow.ToLocalTime(),
                TutorCount = 0
            };

            await _centerRepository.AddAsync(newCenter);
            return newCenter.CenterId;
        }

        public async Task UpdateCenterAsync(Guid id, UpdateCenterRequest request)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));

            var center = await _centerRepository.GetByIdAsync(id) ?? throw new Exception("Center not found.");

            var newName = string.IsNullOrWhiteSpace(request.Name) ? center.Name : request.Name.Trim();
            var newPlaceId = string.IsNullOrWhiteSpace(request.PlaceId) ? center.GooglePlaceId : request.PlaceId;

            if (newName == center.Name && newPlaceId == center.GooglePlaceId)
                return;

            dynamic newPlace = null;
            string formattedAddress = center.FormattedAddress;
            string city = center.City;
            string district = center.District;
            string placeName = center.PlaceName;
            double latitude = center.Latitude ?? 0;
            double longitude = center.Longitude ?? 0;

            if (newPlaceId != center.GooglePlaceId)
            {
                var resp = await _googleMapsService.GetPlaceDetailsAsync(newPlaceId);
                if (!resp.Success || resp.Place == null)
                    throw new Exception("Failed to retrieve new place details from Google Maps.");

                newPlace = resp.Place;

                formattedAddress = GetString(newPlace, "FormattedAddress", "Address", "Vicinity", "formatted_address");
                city = GetString(newPlace, "City", "AdminAreaLevel1", "administrative_area_level_1");
                district = GetString(newPlace, "District", "AdminAreaLevel2", "administrative_area_level_2");
                placeName = GetString(newPlace, "Name", "PlaceName", "name");
                latitude = GetDouble(newPlace, "Lat", "Latitude", "lat", "latitude");
                longitude = GetDouble(newPlace, "Lng", "Longitude", "lng", "longitude");
            }

            var allCenters = await _centerRepository.GetAllAsync();

            foreach (var c in allCenters.Where(c => c.CenterId != id))
            {
                if (c.GooglePlaceId == newPlaceId)
                    throw new Exception($"This Google location is already used by another center: \"{c.Name}\"");

                if (string.Equals(c.Name, newName, StringComparison.OrdinalIgnoreCase) &&
                    string.Equals(c.City, city, StringComparison.OrdinalIgnoreCase) &&
                    string.Equals(c.District, district, StringComparison.OrdinalIgnoreCase))
                    throw new Exception($"A center named \"{newName}\" already exists in {city}, {district}.");

                if (!string.IsNullOrWhiteSpace(c.FormattedAddress) && AreAddressesTooSimilar(c.FormattedAddress, formattedAddress))
                    throw new Exception($"The new address is too similar to existing center: \"{c.Name}\"");

                if (c.Latitude.HasValue && c.Longitude.HasValue && latitude != 0 && longitude != 0)
                {
                    var distance = CalculateDistance(latitude, longitude, c.Latitude.Value, c.Longitude.Value);
                    if (distance <= 0.15)
                    {
                        var meters = Math.Round(distance * 1000);
                        throw new Exception($"New location is too close (~{meters}m) to existing center: \"{c.Name}\"");
                    }
                }
            }

            center.Name = newName;
            if (newPlace != null)
            {
                center.GooglePlaceId = newPlaceId;
                center.FormattedAddress = formattedAddress;
                center.Latitude = latitude == 0 ? null : latitude;
                center.Longitude = longitude == 0 ? null : longitude;
                center.City = city;
                center.District = district;
                center.PlaceName = placeName;
                center.CountryCode = "VN";
                center.LocationUpdatedDate = DateTime.UtcNow.ToLocalTime();
            }

            center.UpdatedDate = DateTime.UtcNow.ToLocalTime();
            await _centerRepository.UpdateAsync(center);
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
                CreatedDate = DateTime.UtcNow.ToLocalTime()
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
        private string GetString(dynamic obj, params string[] possibleNames)
        {
            if (obj == null) return "";
            foreach (var name in possibleNames)
            {
                try
                {
                    var value = obj.GetType().GetProperty(name)?.GetValue(obj);
                    if (value is string str && !string.IsNullOrWhiteSpace(str)) return str;
                }
                catch { /* ignore */ }
            }
            return "";
        }

        private double GetDouble(dynamic obj, params string[] possibleNames)
        {
            if (obj == null) return 0.0;
            foreach (var name in possibleNames)
            {
                try
                {
                    var value = obj.GetType().GetProperty(name)?.GetValue(obj);
                    if (value is double d) return d;
                    if (value is float f) return f;
                    if (value is decimal m) return (double)m;
                }
                catch { /* ignore */ }
            }
            return 0.0;
        }
        private bool AreAddressesTooSimilar(string addr1, string addr2)
        {
            if (string.IsNullOrWhiteSpace(addr1) || string.IsNullOrWhiteSpace(addr2))
                return false;

            var clean1 = NormalizeAddress(addr1);
            var clean2 = NormalizeAddress(addr2);

            var longer = clean1.Length > clean2.Length ? clean1 : clean2;
            var shorter = clean1.Length > clean2.Length ? clean2 : clean1;

            return longer.Contains(shorter, StringComparison.OrdinalIgnoreCase) ||
                   shorter.Contains(longer, StringComparison.OrdinalIgnoreCase) ||
                   ComputeLevenshteinSimilarity(clean1, clean2) >= 0.8;
        }
        private string NormalizeAddress(string addr) => addr
            .Replace("Phường", "", StringComparison.OrdinalIgnoreCase)
            .Replace("Quận", "", StringComparison.OrdinalIgnoreCase)
            .Replace("Thành phố", "", StringComparison.OrdinalIgnoreCase)
            .Replace("Hồ Chí Minh", "HCM", StringComparison.OrdinalIgnoreCase)
            .Replace("TP.", "", StringComparison.OrdinalIgnoreCase)
            .Replace(",", " ")
            .Replace(".", " ")
            .Replace("  ", " ")
            .Trim()
            .ToLowerInvariant();

        private double ComputeLevenshteinSimilarity(string s1, string s2)
        {
            int len1 = s1.Length;
            int len2 = s2.Length;
            var matrix = new int[len1 + 1, len2 + 1];

            for (int i = 0; i <= len1; i++) matrix[i, 0] = i;
            for (int j = 0; j <= len2; j++) matrix[0, j] = j;

            for (int i = 1; i <= len1; i++)
                for (int j = 1; j <= len2; j++)
                {
                    int cost = s1[i - 1] == s2[j - 1] ? 0 : 1;
                    matrix[i, j] = Math.Min(Math.Min(matrix[i - 1, j] + 1, matrix[i, j - 1] + 1), matrix[i - 1, j - 1] + cost);
                }

            int maxLen = Math.Max(len1, len2);
            return maxLen == 0 ? 1.0 : 1.0 - (double)matrix[len1, len2] / maxLen;
        }
    }
}