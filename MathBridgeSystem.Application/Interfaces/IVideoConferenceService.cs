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
}