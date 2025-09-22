using MathBridge.Application.DTOs;

namespace MathBridge.Application.Interfaces;

public interface ILocationService
{
    Task<AddressAutocompleteResponse> GetAddressAutocompleteAsync(string input, string? country = null);
    Task<SaveAddressResponse> SaveUserAddressAsync(Guid userId, SaveAddressRequest request);
    Task<FindNearbyUsersResponse> FindNearbyUsersAsync(Guid currentUserId, int radiusKm = 5);
    Task<GeocodeResponse> GeocodeAddressAsync(string address, string? country = "VN");
    Task<List<CenterDto>> FindCentersNearAddressAsync(string address, double radiusKm = 10.0);
}
