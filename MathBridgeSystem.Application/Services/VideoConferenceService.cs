using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using MathBridgeSystem.Application.DTOs.VideoConference;
using MathBridgeSystem.Application.Interfaces;
using MathBridgeSystem.Domain.Entities;
using MathBridgeSystem.Infrastructure.Data;

namespace MathBridgeSystem.Application.Services;

public class VideoConferenceService : IVideoConferenceService
{
    private readonly MathBridgeDbContext _context;
    private readonly Dictionary<string, IVideoConferenceProvider> _providers;

    public VideoConferenceService(
        MathBridgeDbContext context,
        IEnumerable<IVideoConferenceProvider> providers)
    {
        _context = context;
        _providers = providers.ToDictionary(p => p.PlatformName, p => p);
    }

    public async Task<VideoConferenceSessionDto> CreateVideoConferenceAsync(
        CreateVideoConferenceRequest request, 
        Guid createdByUserId)
    {
        // Validate booking and contract exist
        var booking = await _context.Sessions.FindAsync(request.BookingId);
        if (booking == null)
            throw new Exception("Booking not found");

        var contract = await _context.Contracts.FindAsync(request.ContractId);
        if (contract == null)
            throw new Exception("Contract not found");

        // Get the appropriate provider
        if (!_providers.TryGetValue(request.Platform, out var provider))
            throw new Exception($"Video conference provider '{request.Platform}' not found");

        // Create meeting via provider
        var creationResult = await provider.CreateMeetingAsync( );

        if (!creationResult.Success)
            throw new Exception($"Failed to create meeting: {creationResult.ErrorMessage}");

        // Create conference session in database
        var conferenceSession = new VideoConferenceSession
        {
            ConferenceId = Guid.NewGuid(),
            BookingId = request.BookingId,
            ContractId = request.ContractId,
            Platform = request.Platform,
            SpaceName = creationResult.SpaceName,
            SpaceId = creationResult.MeetingId,
            MeetingUri = creationResult.MeetingUri,
            MeetingCode = creationResult.MeetingCode,
            CreatedByUserId = createdByUserId,
            CreatedDate = DateTime.UtcNow
        };

        _context.VideoConferenceSessions.Add(conferenceSession);

        await _context.SaveChangesAsync();

        return await GetVideoConferenceAsync(conferenceSession.ConferenceId);
    }

    public async Task<VideoConferenceSessionDto> GetVideoConferenceAsync(Guid conferenceId)
    {
        var session = await _context.VideoConferenceSessions
            .FirstOrDefaultAsync(s => s.ConferenceId == conferenceId);

        if (session == null)
            throw new Exception("Video conference session not found");

        return MapToDto(session);
    }

    public async Task<List<VideoConferenceSessionDto>> GetVideoConferencesByBookingAsync(Guid bookingId)
    {
        var sessions = await _context.VideoConferenceSessions
            .Where(s => s.BookingId == bookingId)
            .ToListAsync();

        return sessions.Select(MapToDto).ToList();
    }

    public async Task<List<VideoConferenceSessionDto>> GetVideoConferencesByContractAsync(Guid contractId)
    {
        var sessions = await _context.VideoConferenceSessions
            .Where(s => s.ContractId == contractId)
            .ToListAsync();

        return sessions.Select(MapToDto).ToList();
    }
    
    private VideoConferenceSessionDto MapToDto(VideoConferenceSession session)
    {
        return new VideoConferenceSessionDto
        {
            ConferenceId = session.ConferenceId,
            BookingId = session.BookingId,
            ContractId = session.ContractId,
            Platform = session.Platform,
            SpaceName = session.SpaceName,
            SpaceId = session.SpaceId,
            MeetingUri = session.MeetingUri,
            MeetingCode = session.MeetingCode,
            CreatedByUserId = session.CreatedByUserId,
            CreatedDate = session.CreatedDate,
            UpdatedDate = session.UpdatedDate,
        };
    }
}