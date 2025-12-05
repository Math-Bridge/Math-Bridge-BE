using System.Text.Json.Serialization;

namespace MathBridgeSystem.Application.DTOs
{
    public class CreateContractRequest
    {
        [JsonPropertyName("parentId")] public Guid ParentId { get; set; }
        [JsonPropertyName("childId")] public Guid ChildId { get; set; }
        [JsonPropertyName("secondChildId")] public Guid? SecondChildId { get; set; }
        [JsonPropertyName("packageId")] public Guid PackageId { get; set; }

        [JsonPropertyName("mainTutorId")] public Guid? MainTutorId { get; set; }
        [JsonPropertyName("substituteTutor1Id")] public Guid? SubstituteTutor1Id { get; set; }
        [JsonPropertyName("substituteTutor2Id")] public Guid? SubstituteTutor2Id { get; set; }
        [JsonPropertyName("centerId")] public Guid? CenterId { get; set; }

        [JsonPropertyName("startDate")] public DateOnly StartDate { get; set; }
        [JsonPropertyName("endDate")] public DateOnly EndDate { get; set; }

        [JsonPropertyName("daysOfWeeks")] public int? DaysOfWeeks { get; set; }

        [JsonPropertyName("startTime")] public TimeOnly? StartTime { get; set; }
        [JsonPropertyName("endTime")] public TimeOnly? EndTime { get; set; }
        [JsonPropertyName("isOnline")] public bool IsOnline { get; set; }

        [JsonPropertyName("offlineAddress")] public string? OfflineAddress { get; set; }
        [JsonPropertyName("offlineLatitude")] public decimal? OfflineLatitude { get; set; }
        [JsonPropertyName("offlineLongitude")] public decimal? OfflineLongitude { get; set; }
        [JsonPropertyName("videoCallPlatform")] public string? VideoCallPlatform { get; set; }

        [JsonPropertyName("maxDistanceKm")] public decimal? MaxDistanceKm { get; set; } = 15;

        [JsonPropertyName("status")] public string Status { get; set; } = "unpaid";
    }
}