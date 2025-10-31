using System;
using System.Threading.Tasks;
using MathBridgeSystem.Application.DTOs.VideoConference;

namespace MathBridgeSystem.Application.Interfaces;

public interface IVideoConferenceProvider
{
    string PlatformName { get; }

    Task<VideoConferenceCreationResult> CreateMeetingAsync();
    
    
}



