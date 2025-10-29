using System;
using System.Security.Claims;
using System.Threading.Tasks;
using MathBridgeSystem.Application.DTOs.VideoConference;
using MathBridgeSystem.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MathBridgeSystem.Api.Controllers;

[Route("api/video-conferences")]
[ApiController]
[Authorize]
public class VideoConferenceController : ControllerBase
{
    private readonly IVideoConferenceService _videoConferenceService;

    public VideoConferenceController(IVideoConferenceService videoConferenceService)
    {
        _videoConferenceService = videoConferenceService ?? throw new ArgumentNullException(nameof(videoConferenceService));
    }

    /// <summary>
    /// Create a new video conference session
    /// </summary>
    /// <param name="request">Video conference creation details</param>
    /// <returns>Created video conference session</returns>
    [HttpPost]
    [Authorize(Roles = "tutor,parent,admin")]
    public async Task<IActionResult> CreateVideoConference([FromBody] CreateVideoConferenceRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        try
        {
            var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var conference = await _videoConferenceService.CreateVideoConferenceAsync(request, userId);
            return Ok(conference);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Get video conference session by ID
    /// </summary>
    /// <param name="conferenceId">Conference ID</param>
    /// <returns>Video conference session details</returns>
    [HttpGet("{conferenceId}")]
    public async Task<IActionResult> GetVideoConference(Guid conferenceId)
    {
        try
        {
            var conference = await _videoConferenceService.GetVideoConferenceAsync(conferenceId);
            return Ok(conference);
        }
        catch (Exception ex)
        {
            return NotFound(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Get all video conferences for a specific booking
    /// </summary>
    /// <param name="bookingId">Booking ID</param>
    /// <returns>List of video conference sessions</returns>
    [HttpGet("booking/{bookingId}")]
    public async Task<IActionResult> GetVideoConferencesByBooking(Guid bookingId)
    {
        try
        {
            var conferences = await _videoConferenceService.GetVideoConferencesByBookingAsync(bookingId);
            return Ok(conferences);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Get all video conferences for a specific contract
    /// </summary>
    /// <param name="contractId">Contract ID</param>
    /// <returns>List of video conference sessions</returns>
    [HttpGet("contract/{contractId}")]
    public async Task<IActionResult> GetVideoConferencesByContract(Guid contractId)
    {
        try
        {
            var conferences = await _videoConferenceService.GetVideoConferencesByContractAsync(contractId);
            return Ok(conferences);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Update video conference details
    /// </summary>
    /// <param name="conferenceId">Conference ID</param>
    /// <param name="request">Update details</param>
    /// <returns>Updated video conference session</returns>
    [HttpPut("{conferenceId}")]
    [Authorize(Roles = "tutor,parent,admin")]
    public async Task<IActionResult> UpdateVideoConference(Guid conferenceId, [FromBody] UpdateVideoConferenceRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        try
        {
            var conference = await _videoConferenceService.UpdateVideoConferenceAsync(conferenceId, request);
            return Ok(conference);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Delete a video conference session
    /// </summary>
    /// <param name="conferenceId">Conference ID</param>
    /// <returns>Success status</returns>
    [HttpDelete("{conferenceId}")]
    [Authorize(Roles = "tutor,parent,admin")]
    public async Task<IActionResult> DeleteVideoConference(Guid conferenceId)
    {
        try
        {
            var result = await _videoConferenceService.DeleteVideoConferenceAsync(conferenceId);
            if (result)
                return Ok(new { message = "Video conference deleted successfully" });
            return NotFound(new { error = "Video conference not found" });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Start a video conference session (marks as in progress)
    /// </summary>
    /// <param name="conferenceId">Conference ID</param>
    /// <returns>Updated video conference session</returns>
    [HttpPost("{conferenceId}/start")]
    [Authorize(Roles = "tutor,parent")]
    public async Task<IActionResult> StartVideoConference(Guid conferenceId)
    {
        try
        {
            var conference = await _videoConferenceService.StartVideoConferenceAsync(conferenceId);
            return Ok(conference);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// End a video conference session (marks as completed)
    /// </summary>
    /// <param name="conferenceId">Conference ID</param>
    /// <returns>Updated video conference session</returns>
    [HttpPost("{conferenceId}/end")]
    [Authorize(Roles = "tutor,parent")]
    public async Task<IActionResult> EndVideoConference(Guid conferenceId)
    {
        try
        {
            var conference = await _videoConferenceService.EndVideoConferenceAsync(conferenceId);
            return Ok(conference);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Join a video conference (records participant join time)
    /// </summary>
    /// <param name="conferenceId">Conference ID</param>
    /// <returns>Participant details</returns>
    [HttpPost("{conferenceId}/join")]
    public async Task<IActionResult> JoinVideoConference(Guid conferenceId)
    {
        try
        {
            var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var participant = await _videoConferenceService.JoinVideoConferenceAsync(conferenceId, userId);
            return Ok(participant);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Leave a video conference (records participant leave time and calculates duration)
    /// </summary>
    /// <param name="conferenceId">Conference ID</param>
    /// <returns>Participant details with duration</returns>
    [HttpPost("{conferenceId}/leave")]
    public async Task<IActionResult> LeaveVideoConference(Guid conferenceId)
    {
        try
        {
            var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var participant = await _videoConferenceService.LeaveVideoConferenceAsync(conferenceId, userId);
            return Ok(participant);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }
}