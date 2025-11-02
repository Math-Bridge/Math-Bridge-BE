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
        private readonly ITestResultRepository _testResultRepository;

        public TutorService(
            IUserRepository userRepository,
            ITutorCenterRepository tutorCenterRepository,
            ITutorScheduleRepository tutorScheduleRepository,
            IReviewRepository reviewRepository,
            ITestResultRepository testResultRepository)
        {
            _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
            _tutorCenterRepository = tutorCenterRepository ?? throw new ArgumentNullException(nameof(tutorCenterRepository));
            _tutorScheduleRepository = tutorScheduleRepository ?? throw new ArgumentNullException(nameof(tutorScheduleRepository));
            _reviewRepository = reviewRepository ?? throw new ArgumentNullException(nameof(reviewRepository));
            _testResultRepository = testResultRepository ?? throw new ArgumentNullException(nameof(testResultRepository));
        }

        public async Task<TutorDto> GetTutorByIdAsync(Guid id, Guid currentUserId, string currentUserRole)
        {
            if (string.IsNullOrEmpty(currentUserRole) || (currentUserRole != "admin" && currentUserId != id))
                throw new Exception("Unauthorized access");

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

            // Get test results
            var testResults = await _testResultRepository.GetByTutorIdAsync(id);

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

            // Map TestResults
            tutorDto.TestResults = testResults.Select(tr => new TestResultDetailDto
            {
                ResultId = tr.ResultId,
                ChildId = tr.ChildId,
                ChildName = tr.Child?.FullName ?? "Unknown",
                TutorId = tr.TutorId,
                TestName = tr.TestName,
                TestType = tr.TestType,
                Score = tr.Score,
                MaxScore = tr.MaxScore,
                Percentage = tr.Percentage,
                DurationMinutes = tr.DurationMinutes,
                NumberOfQuestions = tr.NumberOfQuestions,
                CorrectAnswers = tr.CorrectAnswers,
                TestDate = tr.TestDate,
                CurriculumId = tr.CurriculumId
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

            // Update user profile
            user.FullName = request.FullName ?? user.FullName;
            user.PhoneNumber = request.PhoneNumber ?? user.PhoneNumber;
            user.Gender = request.Gender ?? user.Gender;
            user.City = request.City ?? user.City;
            user.District = request.District ?? user.District;
            user.FormattedAddress = request.FormattedAddress ?? user.FormattedAddress;
            
            if (request.Latitude.HasValue)
                user.Latitude = (double)request.Latitude.Value;
            if (request.Longitude.HasValue)
                user.Longitude = (double)request.Longitude.Value;

            // Update TutorVerification if provided
            if (request.TutorVerification != null)
            {
                if (user.TutorVerification == null)
                    user.TutorVerification = new TutorVerification { UserId = id };

                user.TutorVerification.University = request.TutorVerification.University ?? user.TutorVerification.University;
                user.TutorVerification.Major = request.TutorVerification.Major ?? user.TutorVerification.Major;
                user.TutorVerification.Bio = request.TutorVerification.Bio ?? user.TutorVerification.Bio;
                
                if (request.TutorVerification.HourlyRate.HasValue)
                    user.TutorVerification.HourlyRate = request.TutorVerification.HourlyRate.Value;
            }

            await _userRepository.UpdateAsync(user);
            return user.UserId;
        }
    }
}
