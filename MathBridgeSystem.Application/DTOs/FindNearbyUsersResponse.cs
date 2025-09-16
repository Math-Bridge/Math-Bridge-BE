namespace MathBridge.Application.DTOs;

public class FindNearbyUsersResponse
{
    public List<NearbyUser> NearbyUsers { get; set; } = new();
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public int TotalUsers { get; set; }
    public int RadiusKm { get; set; }
}

public class NearbyUser
{
    public Guid UserId { get; set; }
    public string FullName { get; set; }
    public string FormattedAddress { get; set; }
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public double DistanceKm { get; set; }
    public string? City { get; set; }
    public string? District { get; set; }
    public string? Ward { get; set; }
}