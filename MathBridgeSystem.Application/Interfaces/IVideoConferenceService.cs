using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MathBridgeSystem.Application.DTOs.VideoConference;

namespace MathBridgeSystem.Application.Interfaces;

public interface IVideoConferenceService
{
    Task<VideoConferenceSessionDto> CreateVideoConferenceAsync(CreateVideoConferenceRequest request, Guid createdByUserId);
    Task<VideoConferenceSessionDto> GetVideoConferenceAsync(Guid conferenceId);
    Task<List<VideoConferenceSessionDto>> GetVideoConferencesByBookingAsync(Guid bookingId);
    Task<List<VideoConferenceSessionDto>> GetVideoConferencesByContractAsync(Guid contractId);
    Task<VideoConferenceSessionDto> UpdateVideoConferenceAsync(Guid conferenceId, UpdateVideoConferenceRequest request);
    Task<bool> DeleteVideoConferenceAsync(Guid conferenceId);
    Task<VideoConferenceSessionDto> StartVideoConferenceAsync(Guid conferenceId);
    Task<VideoConferenceSessionDto> EndVideoConferenceAsync(Guid conferenceId);
    Task<VideoConferenceParticipantDto> JoinVideoConferenceAsync(Guid conferenceId, Guid userId);
    Task<VideoConferenceParticipantDto> LeaveVideoConferenceAsync(Guid conferenceId, Guid userId);
}