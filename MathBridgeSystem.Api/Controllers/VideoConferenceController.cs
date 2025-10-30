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
}