using MathBridgeSystem.Application.DTOs.TutorVerification;

namespace MathBridgeSystem.Application.DTOs
{
    public class TutorDto
    {
        public Guid UserId { get; set; }
        public string FullName { get; set; }
        public string Email { get; set; }
        public string PhoneNumber { get; set; }
        public string Gender { get; set; }
        public decimal WalletBalance { get; set; }
        public string Status { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime? LastActive { get; set; }
        public string FormattedAddress { get; set; }
        public string City { get; set; }
        public string District { get; set; }
        public decimal? Latitude { get; set; }
        public decimal? Longitude { get; set; }
        //add new fields for average rating and feedback count
        public decimal AverageRating { get; set; } = 0m;
        public int FeedbackCount { get; set; } = 0;

        // TutorVerification
        public TutorVerificationDto TutorVerification { get; set; }

        // Related TutorCenters and Centers
        public List<TutorCenterDetailDto> TutorCenters { get; set; }

        // TutorSchedules
        public List<TutorScheduleDetailDto> TutorSchedules { get; set; }

        // Final Feedbacks
        public List<FinalFeedbackDetailDto> FinalFeedbacks { get; set; }
    }

    public class TutorVerificationDto
    {
        public Guid VerificationId { get; set; }
        public string University { get; set; }
        public string Major { get; set; }
        public decimal HourlyRate { get; set; }
        public string Bio { get; set; }
        public string VerificationStatus { get; set; }
        public DateTime? VerificationDate { get; set; }
        public DateTime CreatedDate { get; set; }
    }

    public class TutorCenterDetailDto
    {
        public Guid TutorCenterId { get; set; }
        public Guid CenterId { get; set; }
        public DateTime CreatedDate { get; set; }
        public CenterDetailDto Center { get; set; }
    }

    public class CenterDetailDto
    {
        public Guid CenterId { get; set; }
        public string Name { get; set; }
        public decimal? Latitude { get; set; }
        public decimal? Longitude { get; set; }
        public string FormattedAddress { get; set; }
        public string City { get; set; }
        public string District { get; set; }
        public string GooglePlaceId { get; set; }
        public int TutorCount { get; set; }
        public DateTime CreatedDate { get; set; }
    }

    public class TutorScheduleDetailDto
    {
        public Guid AvailabilityId { get; set; }
        public byte DaysOfWeek { get; set; }
        public string AvailableFrom { get; set; }
        public string AvailableUntil { get; set; }
        public string EffectiveFrom { get; set; }
        public string EffectiveUntil { get; set; }
        public bool CanTeachOnline { get; set; }
        public bool CanTeachOffline { get; set; }
        public bool? IsBooked { get; set; }
        public string Status { get; set; }
        public DateTime CreatedDate { get; set; }
    }

    public class FinalFeedbackDetailDto
    {
        public Guid FeedbackId { get; set; }
        public Guid UserId { get; set; }
        public Guid ContractId { get; set; }
        public string FeedbackProviderType { get; set; }
        public string? FeedbackText { get; set; }
        public int OverallSatisfactionRating { get; set; }
        public bool WouldRecommend { get; set; }
        public string FeedbackStatus { get; set; }
        public DateTime CreatedDate { get; set; }
        public string ProviderName { get; set; }
    }

    public class UpdateTutorRequest
    {
        public string FullName { get; set; }
        public string PhoneNumber { get; set; }
        public string Gender { get; set; }

        // TutorVerification updates - using the standalone DTO from TutorVerification folder
        public UpdateTutorVerificationRequest TutorVerification { get; set; }
    }
}
