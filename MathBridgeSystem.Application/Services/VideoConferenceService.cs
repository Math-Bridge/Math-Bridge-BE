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
        var displayName = request.DisplayName ?? $"Math Bridge Session - {booking.BookingId}";
        var creationResult = await provider.CreateMeetingAsync(
            displayName,
            request.ScheduledStartTime,
            request.ScheduledEndTime);

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
            DisplayName = displayName,
            ScheduledStartTime = request.ScheduledStartTime,
            ScheduledEndTime = request.ScheduledEndTime,
            Status = "Scheduled",
            CreatedByUserId = createdByUserId,
            CreatedDate = DateTime.UtcNow
        };

        _context.VideoConferenceSessions.Add(conferenceSession);

        // Add participants if provided
        if (request.ParticipantUserIds != null && request.ParticipantUserIds.Any())
        {
            foreach (var userId in request.ParticipantUserIds)
            {
                var participant = new VideoConferenceParticipant
                {
                    ParticipantId = Guid.NewGuid(),
                    ConferenceId = conferenceSession.ConferenceId,
                    UserId = userId,
                    ParticipantType = userId == contract.ChildId ? "Student" : "Tutor",
                    Status = "Invited",
                    CreatedDate = DateTime.UtcNow
                };
                _context.VideoConferenceParticipants.Add(participant);
            }
        }

        await _context.SaveChangesAsync();

        return await GetVideoConferenceAsync(conferenceSession.ConferenceId);
    }

    public async Task<VideoConferenceSessionDto> GetVideoConferenceAsync(Guid conferenceId)
    {
        var session = await _context.VideoConferenceSessions
            .Include(s => s.VideoConferenceParticipants)
            .ThenInclude(p => p.User)
            .FirstOrDefaultAsync(s => s.ConferenceId == conferenceId);

        if (session == null)
            throw new Exception("Video conference session not found");

        return MapToDto(session);
    }

    public async Task<List<VideoConferenceSessionDto>> GetVideoConferencesByBookingAsync(Guid bookingId)
    {
        var sessions = await _context.VideoConferenceSessions
            .Include(s => s.VideoConferenceParticipants)
            .ThenInclude(p => p.User)
            .Where(s => s.BookingId == bookingId)
            .ToListAsync();

        return sessions.Select(MapToDto).ToList();
    }

    public async Task<List<VideoConferenceSessionDto>> GetVideoConferencesByContractAsync(Guid contractId)
    {
        var sessions = await _context.VideoConferenceSessions
            .Include(s => s.VideoConferenceParticipants)
            .ThenInclude(p => p.User)
            .Where(s => s.ContractId == contractId)
            .ToListAsync();

        return sessions.Select(MapToDto).ToList();
    }

    public async Task<VideoConferenceSessionDto> UpdateVideoConferenceAsync(
        Guid conferenceId, 
        UpdateVideoConferenceRequest request)
    {
        var session = await _context.VideoConferenceSessions.FindAsync(conferenceId);
        if (session == null)
            throw new Exception("Video conference session not found");

        if (request.ScheduledStartTime.HasValue)
            session.ScheduledStartTime = request.ScheduledStartTime.Value;

        if (request.ScheduledEndTime.HasValue)
            session.ScheduledEndTime = request.ScheduledEndTime.Value;

        if (!string.IsNullOrEmpty(request.DisplayName))
            session.DisplayName = request.DisplayName;

        if (!string.IsNullOrEmpty(request.Status))
            session.Status = request.Status;

        session.UpdatedDate = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return await GetVideoConferenceAsync(conferenceId);
    }

    public async Task<bool> DeleteVideoConferenceAsync(Guid conferenceId)
    {
        var session = await _context.VideoConferenceSessions.FindAsync(conferenceId);
        if (session == null)
            return false;

        // Delete meeting from provider if possible
        if (_providers.TryGetValue(session.Platform, out var provider) && !string.IsNullOrEmpty(session.SpaceId))
        {
            try
            {
                await provider.DeleteMeetingAsync(session.SpaceId);
            }
            catch
            {
                // Continue even if provider deletion fails
            }
        }

        _context.VideoConferenceSessions.Remove(session);
        await _context.SaveChangesAsync();

        return true;
    }

    public async Task<VideoConferenceSessionDto> StartVideoConferenceAsync(Guid conferenceId)
    {
        var session = await _context.VideoConferenceSessions.FindAsync(conferenceId);
        if (session == null)
            throw new Exception("Video conference session not found");

        session.ActualStartTime = DateTime.UtcNow;
        session.Status = "InProgress";
        session.UpdatedDate = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return await GetVideoConferenceAsync(conferenceId);
    }

    public async Task<VideoConferenceSessionDto> EndVideoConferenceAsync(Guid conferenceId)
    {
        var session = await _context.VideoConferenceSessions.FindAsync(conferenceId);
        if (session == null)
            throw new Exception("Video conference session not found");

        session.ActualEndTime = DateTime.UtcNow;
        session.Status = "Completed";
        session.UpdatedDate = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return await GetVideoConferenceAsync(conferenceId);
    }

    public async Task<VideoConferenceParticipantDto> JoinVideoConferenceAsync(Guid conferenceId, Guid userId)
    {
        var participant = await _context.VideoConferenceParticipants
            .FirstOrDefaultAsync(p => p.ConferenceId == conferenceId && p.UserId == userId);

        if (participant == null)
        {
            // Create new participant if not exists
            var session = await _context.VideoConferenceSessions
                .Include(s => s.Contract)
                .FirstOrDefaultAsync(s => s.ConferenceId == conferenceId);

            if (session == null)
                throw new Exception("Video conference session not found");

            participant = new VideoConferenceParticipant
            {
                ParticipantId = Guid.NewGuid(),
                ConferenceId = conferenceId,
                UserId = userId,
                ParticipantType = userId == session.Contract.ChildId ? "Student" : "Tutor",
                Status = "Joined",
                CreatedDate = DateTime.UtcNow
            };
            _context.VideoConferenceParticipants.Add(participant);
        }

        participant.JoinedAt = DateTime.UtcNow;
        participant.Status = "Joined";
        participant.UpdatedDate = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        var user = await _context.Users.FindAsync(userId);
        return new VideoConferenceParticipantDto
        {
            ParticipantId = participant.ParticipantId,
            ConferenceId = participant.ConferenceId,
            UserId = participant.UserId,
            ParticipantType = participant.ParticipantType,
            JoinedAt = participant.JoinedAt,
            LeftAt = participant.LeftAt,
            DurationMinutes = participant.DurationMinutes,
            Status = participant.Status,
            UserName = user?.FullName,
            UserEmail = user?.Email
        };
    }

    public async Task<VideoConferenceParticipantDto> LeaveVideoConferenceAsync(Guid conferenceId, Guid userId)
    {
        var participant = await _context.VideoConferenceParticipants
            .FirstOrDefaultAsync(p => p.ConferenceId == conferenceId && p.UserId == userId);

        if (participant == null)
            throw new Exception("Participant not found in this conference");

        participant.LeftAt = DateTime.UtcNow;
        participant.Status = "Left";

        if (participant.JoinedAt.HasValue)
        {
            var duration = (participant.LeftAt.Value - participant.JoinedAt.Value).TotalMinutes;
            participant.DurationMinutes = (int)Math.Round(duration);
        }

        participant.UpdatedDate = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        var user = await _context.Users.FindAsync(userId);
        return new VideoConferenceParticipantDto
        {
            ParticipantId = participant.ParticipantId,
            ConferenceId = participant.ConferenceId,
            UserId = participant.UserId,
            ParticipantType = participant.ParticipantType,
            JoinedAt = participant.JoinedAt,
            LeftAt = participant.LeftAt,
            DurationMinutes = participant.DurationMinutes,
            Status = participant.Status,
            UserName = user?.FullName,
            UserEmail = user?.Email
        };
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
            DisplayName = session.DisplayName,
            ScheduledStartTime = session.ScheduledStartTime,
            ScheduledEndTime = session.ScheduledEndTime,
            ActualStartTime = session.ActualStartTime,
            ActualEndTime = session.ActualEndTime,
            Status = session.Status,
            CreatedByUserId = session.CreatedByUserId,
            CreatedDate = session.CreatedDate,
            UpdatedDate = session.UpdatedDate,
            Participants = session.VideoConferenceParticipants?.Select(p => new VideoConferenceParticipantDto
            {
                ParticipantId = p.ParticipantId,
                ConferenceId = p.ConferenceId,
                UserId = p.UserId,
                ParticipantType = p.ParticipantType,
                JoinedAt = p.JoinedAt,
                LeftAt = p.LeftAt,
                DurationMinutes = p.DurationMinutes,
                Status = p.Status,
                UserName = p.User?.FullName,
                UserEmail = p.User?.Email
            }).ToList()
        };
    }
}