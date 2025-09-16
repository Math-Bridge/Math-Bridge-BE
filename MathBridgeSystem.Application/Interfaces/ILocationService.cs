using MathBridge.Application.DTOs;

namespace MathBridge.Application.Interfaces;

public interface ILocationService
{
    Task<AddressAutocompleteResponse> GetAddressAutocompleteAsync(string input, string? country = null);
    Task<SaveAddressResponse> SaveUserAddressAsync(Guid userId, SaveAddressRequest request);
    Task<FindNearbyUsersResponse> FindNearbyUsersAsync(Guid currentUserId, int radiusKm = 5);
}