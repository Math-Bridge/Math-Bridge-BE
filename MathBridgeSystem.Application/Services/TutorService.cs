using MathBridgeSystem.Application.DTOs;
using MathBridgeSystem.Application.Interfaces;
using MathBridgeSystem.Domain.Entities;
using MathBridgeSystem.Domain.Interfaces;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace MathBridgeSystem.Application.Services
{
    public class TutorService : ITutorService
    {
        private readonly IUserRepository _userRepository;
        private readonly ITutorCenterRepository _tutorCenterRepository;
        private readonly ITutorScheduleRepository _tutorScheduleRepository;
        private readonly IReviewRepository _reviewRepository;

        public TutorService(
            IUserRepository userRepository,
            ITutorCenterRepository tutorCenterRepository,
            ITutorScheduleRepository tutorScheduleRepository,
            IReviewRepository reviewRepository)
        {
            _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
            _tutorCenterRepository = tutorCenterRepository ?? throw new ArgumentNullException(nameof(tutorCenterRepository));
            _tutorScheduleRepository = tutorScheduleRepository ?? throw new ArgumentNullException(nameof(tutorScheduleRepository));
            _reviewRepository = reviewRepository ?? throw new ArgumentNullException(nameof(reviewRepository));
        }

        public async Task<TutorDto> GetTutorByIdAsync(Guid id, Guid currentUserId, string currentUserRole)
        {
            var user = await _userRepository.GetByIdAsync(id);
            if (user == null)
                throw new Exception("Tutor not found");

            if (user.Role?.RoleName != "tutor")
                throw new Exception("User is not a tutor");

            // Get tutor centers
            var tutorCenters = await _tutorCenterRepository.GetByTutorIdAsync(id);

            // Get tutor schedules
            var tutorSchedules = await _tutorScheduleRepository.GetByTutorIdAsync(id);

            // Get reviews where user was reviewed
            var reviews = await _reviewRepository.GetByReviewedUserIdAsync(id);

            // Build DTO
            var tutorDto = new TutorDto
            {
                UserId = user.UserId,
                FullName = user.FullName,
                Email = user.Email,
                PhoneNumber = user.PhoneNumber,
                Gender = user.Gender,
                WalletBalance = user.WalletBalance,
                Status = user.Status,
                CreatedDate = user.CreatedDate,
                LastActive = user.LastActive,
                FormattedAddress = user.FormattedAddress,
                City = user.City,
                District = user.District,
                Latitude = user.Latitude.HasValue ? (decimal)user.Latitude.Value : (decimal?)null,
                Longitude = user.Longitude.HasValue ? (decimal)user.Longitude.Value : (decimal?)null
            };

            // Map TutorVerification
            if (user.TutorVerification != null)
            {
                tutorDto.TutorVerification = new TutorVerificationDto
                {
                    VerificationId = user.TutorVerification.VerificationId,
                    University = user.TutorVerification.University,
                    Major = user.TutorVerification.Major,
                    HourlyRate = user.TutorVerification.HourlyRate,
                    Bio = user.TutorVerification.Bio,
                    VerificationStatus = user.TutorVerification.VerificationStatus,
                    VerificationDate = user.TutorVerification.VerificationDate,
                    CreatedDate = user.TutorVerification.CreatedDate
                };
            }

            // Map TutorCenters with Centers
            tutorDto.TutorCenters = tutorCenters.Select(tc => new TutorCenterDetailDto
            {
                TutorCenterId = tc.TutorCenterId,
                CenterId = tc.CenterId,
                CreatedDate = tc.CreatedDate,
                Center = tc.Center != null ? new CenterDetailDto
                {
                    CenterId = tc.Center.CenterId,
                    Name = tc.Center.Name,
                    Latitude = tc.Center.Latitude.HasValue ? (decimal)tc.Center.Latitude.Value : (decimal?)null,
                    Longitude = tc.Center.Longitude.HasValue ? (decimal)tc.Center.Longitude.Value : (decimal?)null,
                    FormattedAddress = tc.Center.FormattedAddress,
                    City = tc.Center.City,
                    District = tc.Center.District,
                    GooglePlaceId = tc.Center.GooglePlaceId,
                    TutorCount = tc.Center.TutorCount,
                    CreatedDate = tc.Center.CreatedDate
                } : null
            }).ToList();

            // Map TutorSchedules
            tutorDto.TutorSchedules = tutorSchedules.Select(ts => new TutorScheduleDetailDto
            {
                AvailabilityId = ts.AvailabilityId,
                DaysOfWeek = ts.DaysOfWeek,
                AvailableFrom = ts.AvailableFrom.ToString(),
                AvailableUntil = ts.AvailableUntil.ToString(),
                EffectiveFrom = ts.EffectiveFrom.ToString(),
                EffectiveUntil = ts.EffectiveUntil?.ToString(),
                CanTeachOnline = ts.CanTeachOnline,
                CanTeachOffline = ts.CanTeachOffline,
                IsBooked = ts.IsBooked,
                Status = ts.Status,
                CreatedDate = ts.CreatedDate
            }).ToList();

            // Map Reviews
            tutorDto.Reviews = reviews.Select(r => new ReviewDetailDto
            {
                ReviewId = r.ReviewId,
                UserId = r.UserId,
                Rating = r.Rating,
                ReviewTitle = r.ReviewTitle,
                ReviewText = r.ReviewText,
                ReviewStatus = r.ReviewStatus,
                CreatedDate = r.CreatedDate,
                ReviewerName = r.User?.FullName ?? "Anonymous"
            }).ToList();

            return tutorDto;
        }

        public async Task<Guid> UpdateTutorAsync(Guid id, UpdateTutorRequest request, Guid currentUserId, string currentUserRole)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            if (string.IsNullOrEmpty(currentUserRole) || (currentUserRole != "admin" && currentUserId != id))
                throw new Exception("Unauthorized access");

            var user = await _userRepository.GetByIdAsync(id);
            if (user == null)
                throw new Exception("Tutor not found");

            if (user.Role?.RoleName != "tutor")
                throw new Exception("User is not a tutor");

            // Update user profile (without location data)
            user.FullName = request.FullName ?? user.FullName;
            
            // Validate gender before updating - only update if provided and valid
            if (!string.IsNullOrEmpty(request.Gender))
            {
                var validGenders = new[] { "Male", "Female", "Other" };
                if (!validGenders.Contains(request.Gender))
                    throw new Exception($"Invalid gender value. Allowed values are: {string.Join(", ", validGenders)}");
                user.Gender = request.Gender;
            }

            // Update TutorVerification if provided
            if (request.TutorVerification != null)
            {
                // Check if any field in TutorVerification has data
                bool hasVerificationData = !string.IsNullOrEmpty(request.TutorVerification.University) ||
                                          !string.IsNullOrEmpty(request.TutorVerification.Major) ||
                                          !string.IsNullOrEmpty(request.TutorVerification.Bio) ||
                                          request.TutorVerification.HourlyRate.HasValue;

                if (user.TutorVerification == null)
                {
                    // Only create new TutorVerification if there's actual data to save
                    if (hasVerificationData)
                    {
                        user.TutorVerification = new TutorVerification
                        {
                            UserId = id,
                            VerificationStatus = "Pending",
                            University = request.TutorVerification.University,
                            Major = request.TutorVerification.Major,
                            Bio = request.TutorVerification.Bio,
                            HourlyRate = request.TutorVerification.HourlyRate ?? 0
                        };
                    }
                }
                else
                {
                    // Update existing TutorVerification
                    user.TutorVerification.University = request.TutorVerification.University ?? user.TutorVerification.University;
                    user.TutorVerification.Major = request.TutorVerification.Major ?? user.TutorVerification.Major;
                    user.TutorVerification.Bio = request.TutorVerification.Bio ?? user.TutorVerification.Bio;
                    
                    if (request.TutorVerification.HourlyRate.HasValue)
                        user.TutorVerification.HourlyRate = request.TutorVerification.HourlyRate.Value;
                }
            }

            await _userRepository.UpdateAsync(user);
            return user.UserId;
        }

        public async Task<List<TutorDto>> GetAllTutorsAsync()
        {
            var tutors = await _userRepository.GetAllAsync();
            var tutorList = new List<TutorDto>();

            foreach (var user in tutors)
            {
                if (user.Role?.RoleName != "tutor")
                    continue;

                // Get tutor centers
                var tutorCenters = await _tutorCenterRepository.GetByTutorIdAsync(user.UserId);

                // Get tutor schedules
                var tutorSchedules = await _tutorScheduleRepository.GetByTutorIdAsync(user.UserId);

                // Get reviews where user was reviewed
                var reviews = await _reviewRepository.GetByReviewedUserIdAsync(user.UserId);

                // Build DTO
                var tutorDto = new TutorDto
                {
                    UserId = user.UserId,
                    FullName = user.FullName,
                    Email = user.Email,
                    PhoneNumber = user.PhoneNumber,
                    Gender = user.Gender,
                    WalletBalance = user.WalletBalance,
                    Status = user.Status,
                    CreatedDate = user.CreatedDate,
                    LastActive = user.LastActive,
                    FormattedAddress = user.FormattedAddress,
                    City = user.City,
                    District = user.District,
                    Latitude = user.Latitude.HasValue ? (decimal)user.Latitude.Value : (decimal?)null,
                    Longitude = user.Longitude.HasValue ? (decimal)user.Longitude.Value : (decimal?)null
                };

                // Map TutorVerification
                if (user.TutorVerification != null)
                {
                    tutorDto.TutorVerification = new TutorVerificationDto
                    {
                        VerificationId = user.TutorVerification.VerificationId,
                        University = user.TutorVerification.University,
                        Major = user.TutorVerification.Major,
                        HourlyRate = user.TutorVerification.HourlyRate,
                        Bio = user.TutorVerification.Bio,
                        VerificationStatus = user.TutorVerification.VerificationStatus,
                        VerificationDate = user.TutorVerification.VerificationDate,
                        CreatedDate = user.TutorVerification.CreatedDate
                    };
                }

                // Map TutorCenters with Centers
                tutorDto.TutorCenters = tutorCenters.Select(tc => new TutorCenterDetailDto
                {
                    TutorCenterId = tc.TutorCenterId,
                    CenterId = tc.CenterId,
                    CreatedDate = tc.CreatedDate,
                    Center = tc.Center != null ? new CenterDetailDto
                    {
                        CenterId = tc.Center.CenterId,
                        Name = tc.Center.Name,
                        Latitude = tc.Center.Latitude.HasValue ? (decimal)tc.Center.Latitude.Value : (decimal?)null,
                        Longitude = tc.Center.Longitude.HasValue ? (decimal)tc.Center.Longitude.Value : (decimal?)null,
                        FormattedAddress = tc.Center.FormattedAddress,
                        City = tc.Center.City,
                        District = tc.Center.District,
                        GooglePlaceId = tc.Center.GooglePlaceId,
                        TutorCount = tc.Center.TutorCount,
                        CreatedDate = tc.Center.CreatedDate
                    } : null
                }).ToList();

                // Map TutorSchedules
                tutorDto.TutorSchedules = tutorSchedules.Select(ts => new TutorScheduleDetailDto
                {
                    AvailabilityId = ts.AvailabilityId,
                    DaysOfWeek = ts.DaysOfWeek,
                    AvailableFrom = ts.AvailableFrom.ToString(),
                    AvailableUntil = ts.AvailableUntil.ToString(),
                    EffectiveFrom = ts.EffectiveFrom.ToString(),
                    EffectiveUntil = ts.EffectiveUntil?.ToString(),
                    CanTeachOnline = ts.CanTeachOnline,
                    CanTeachOffline = ts.CanTeachOffline,
                    IsBooked = ts.IsBooked,
                    Status = ts.Status,
                    CreatedDate = ts.CreatedDate
                }).ToList();

                // Map Reviews
                tutorDto.Reviews = reviews.Select(r => new ReviewDetailDto
                {
                    ReviewId = r.ReviewId,
                    UserId = r.UserId,
                    Rating = r.Rating,
                    ReviewTitle = r.ReviewTitle,
                    ReviewText = r.ReviewText,
                    ReviewStatus = r.ReviewStatus,
                    CreatedDate = r.CreatedDate,
                    ReviewerName = r.User?.FullName ?? "Anonymous"
                }).ToList();

                tutorList.Add(tutorDto);
            }

            return tutorList;
        }
    }
}
