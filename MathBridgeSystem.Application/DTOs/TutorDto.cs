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

        // TutorVerification
        public TutorVerificationDto TutorVerification { get; set; }

        // Related TutorCenters and Centers
        public List<TutorCenterDetailDto> TutorCenters { get; set; }

        // TutorSchedules
        public List<TutorScheduleDetailDto> TutorSchedules { get; set; }

        // Reviews
        public List<ReviewDetailDto> Reviews { get; set; }

        // TestResults
        public List<TestResultDetailDto> TestResults { get; set; }
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

    public class ReviewDetailDto
    {
        public Guid ReviewId { get; set; }
        public Guid UserId { get; set; }
        public int Rating { get; set; }
        public string ReviewTitle { get; set; }
        public string ReviewText { get; set; }
        public string ReviewStatus { get; set; }
        public DateTime CreatedDate { get; set; }
        public string ReviewerName { get; set; }
    }

    public class TestResultDetailDto
    {
        public Guid ResultId { get; set; }
        public Guid ChildId { get; set; }
        public string ChildName { get; set; }
        public Guid TutorId { get; set; }
        public string TestName { get; set; }
        public string TestType { get; set; }
        public decimal Score { get; set; }
        public decimal MaxScore { get; set; }
        public decimal? Percentage { get; set; }
        public int? DurationMinutes { get; set; }
        public int? NumberOfQuestions { get; set; }
        public int? CorrectAnswers { get; set; }
        public DateTime TestDate { get; set; }
        public Guid CurriculumId { get; set; }
    }

    public class UpdateTutorRequest
    {
        public string FullName { get; set; }
        public string PhoneNumber { get; set; }
        public string Gender { get; set; }
        public string City { get; set; }
        public string District { get; set; }
        public string FormattedAddress { get; set; }
        public decimal? Latitude { get; set; }
        public decimal? Longitude { get; set; }

        // TutorVerification updates
        public UpdateTutorVerificationRequest TutorVerification { get; set; }
    }

    public class UpdateTutorVerificationRequest
    {
        public string University { get; set; }
        public string Major { get; set; }
        public decimal? HourlyRate { get; set; }
        public string Bio { get; set; }
    }
}
