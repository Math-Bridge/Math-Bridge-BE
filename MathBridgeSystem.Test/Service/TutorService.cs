﻿using MathBridgeSystem.Application.DTOs;
using MathBridgeSystem.Application.Interfaces;
using MathBridgeSystem.Domain.Entities;
using MathBridgeSystem.Domain.Interfaces;

namespace MathBridgeSystem.Application.Services
{
    public class TutorService : ITutorService
    {
        private readonly IUserRepository _userRepository;
        private readonly ITutorCenterRepository _tutorCenterRepository;
        private readonly ITutorScheduleRepository _tutorScheduleRepository;
        private readonly IFinalFeedbackRepository _finalFeedbackRepository;

        public TutorService(
          IUserRepository userRepository,
          ITutorCenterRepository tutorCenterRepository,
          ITutorScheduleRepository tutorScheduleRepository,
          IFinalFeedbackRepository finalFeedbackRepository)
        {
            _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
            _tutorCenterRepository = tutorCenterRepository ?? throw new ArgumentNullException(nameof(tutorCenterRepository));
            _tutorScheduleRepository = tutorScheduleRepository ?? throw new ArgumentNullException(nameof(tutorScheduleRepository));
            _finalFeedbackRepository = finalFeedbackRepository ?? throw new ArgumentNullException(nameof(finalFeedbackRepository));
        }

        public async Task<TutorDto> GetTutorByIdAsync(Guid id, Guid currentUserId, string currentUserRole)
        {
            var user = await _userRepository.GetByIdAsync(id);
            if (user == null)
                throw new Exception("Tutor not found");

            if (user.Role?.RoleName != "tutor")
                throw new Exception("User is not a tutor");

            var tutorCenters = await _tutorCenterRepository.GetByTutorIdAsync(id);

            var tutorSchedules = await _tutorScheduleRepository.GetByTutorIdAsync(id);

            var finalFeedbacks = await _finalFeedbackRepository.GetByUserIdAsync(id);

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

            tutorDto.FinalFeedbacks = finalFeedbacks.Select(f => new FinalFeedbackDetailDto
            {
                FeedbackId = f.FeedbackId,
                UserId = f.UserId,
                ContractId = f.ContractId,
                FeedbackProviderType = f.FeedbackProviderType,
                FeedbackText = f.FeedbackText,
                OverallSatisfactionRating = f.OverallSatisfactionRating,
                WouldRecommend = f.WouldRecommend,
                FeedbackStatus = f.FeedbackStatus,
                CreatedDate = f.CreatedDate,
                ProviderName = f.User?.FullName ?? "Anonymous"
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

            user.FullName = request.FullName ?? user.FullName;

            if (!string.IsNullOrEmpty(request.Gender))
            {
                var validGenders = new[] { "Male", "Female", "Other" };
                if (!validGenders.Contains(request.Gender))
                    throw new Exception($"Invalid gender value. Allowed values are: {string.Join(", ", validGenders)}");
                user.Gender = request.Gender;
            }

            if (request.TutorVerification != null)
            {
                bool hasVerificationData = !string.IsNullOrEmpty(request.TutorVerification.University) ||
                     !string.IsNullOrEmpty(request.TutorVerification.Major) ||
                     !string.IsNullOrEmpty(request.TutorVerification.Bio) ||
                     request.TutorVerification.HourlyRate.HasValue;

                if (user.TutorVerification == null)
                {

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

                var tutorCenters = await _tutorCenterRepository.GetByTutorIdAsync(user.UserId);

                var tutorSchedules = await _tutorScheduleRepository.GetByTutorIdAsync(user.UserId);

                var finalFeedbacks = await _finalFeedbackRepository.GetByUserIdAsync(user.UserId);

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

                tutorDto.FinalFeedbacks = finalFeedbacks.Select(f => new FinalFeedbackDetailDto
                {
                    FeedbackId = f.FeedbackId,
                    UserId = f.UserId,
                    ContractId = f.ContractId,
                    FeedbackProviderType = f.FeedbackProviderType,
                    FeedbackText = f.FeedbackText,
                    OverallSatisfactionRating = f.OverallSatisfactionRating,
                    WouldRecommend = f.WouldRecommend,
                    FeedbackStatus = f.FeedbackStatus,
                    CreatedDate = f.CreatedDate,
                    ProviderName = f.User?.FullName ?? "Anonymous"
                }).ToList();

                tutorList.Add(tutorDto);
            }

            return tutorList;
        }
    }
}