using MathBridgeSystem.Application.DTOs;

namespace MathBridgeSystem.Application.Interfaces
{
    public interface ITutorService
    {
        Task<TutorDto> GetTutorByIdAsync(Guid id, Guid currentUserId, string currentUserRole);
        Task<Guid> UpdateTutorAsync(Guid id, UpdateTutorRequest request, Guid currentUserId, string currentUserRole);
        Task<List<TutorDto>> GetAllTutorsAsync();
    }
}
