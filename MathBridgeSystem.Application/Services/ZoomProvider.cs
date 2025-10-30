using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using MathBridgeSystem.Application.Interfaces;

namespace MathBridgeSystem.Infrastructure.Services;

public class ZoomProvider : IVideoConferenceProvider
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly string _apiBaseUrl = "https://api.zoom.us/v2";

    public string PlatformName => "Zoom";

    public ZoomProvider(HttpClient httpClient, IConfiguration configuration)
    {
        _httpClient = httpClient;
        _configuration = configuration;
    }

    public async Task<VideoConferenceCreationResult> CreateMeetingAsync()
    {
        try
        {
            var accessToken = await GetAccessTokenAsync();
            var userId = _configuration["Zoom:UserId"] ?? "me";

            var requestBody = new
            {
                type = 2, // Scheduled meeting
                timezone = "UTC",
                settings = new
                {
                    host_video = true,
                    participant_video = true,
                    join_before_host = true,
                    mute_upon_entry = false,
                    watermark = false,
                    audio = "both",
                    auto_recording = "none"
                }
            };

            var request = new HttpRequestMessage(HttpMethod.Post, $"{_apiBaseUrl}/users/{userId}/meetings");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            request.Content = new StringContent(
                JsonSerializer.Serialize(requestBody),
                Encoding.UTF8,
                "application/json");

            var response = await _httpClient.SendAsync(request);
            var responseContent = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                return new VideoConferenceCreationResult
                {
                    Success = false,
                    ErrorMessage = $"Zoom API error: {response.StatusCode} - {responseContent}"
                };
            }

            var meetingData = JsonSerializer.Deserialize<ZoomMeetingResponse>(responseContent, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (meetingData == null)
            {
                return new VideoConferenceCreationResult
                {
                    Success = false,
                    ErrorMessage = "Failed to parse Zoom response"
                };
            }

            return new VideoConferenceCreationResult
            {
                Success = true,
                MeetingId = meetingData.Id.ToString(),
                MeetingUri = meetingData.JoinUrl ?? string.Empty,
                MeetingCode = meetingData.Id.ToString()
            };
        }
        catch (Exception ex)
        {
            return new VideoConferenceCreationResult
            {
                Success = false,
                ErrorMessage = $"Exception creating Zoom meeting: {ex.Message}"
            };
        }
    }

    public async Task<bool> DeleteMeetingAsync(string meetingId)
    {
        try
        {
            var accessToken = await GetAccessTokenAsync();

            var request = new HttpRequestMessage(HttpMethod.Delete, $"{_apiBaseUrl}/meetings/{meetingId}");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

            var response = await _httpClient.SendAsync(request);
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    public async Task<MeetingDetailsResult> GetMeetingDetailsAsync(string meetingId)
    {
        try
        {
            var accessToken = await GetAccessTokenAsync();

            var request = new HttpRequestMessage(HttpMethod.Get, $"{_apiBaseUrl}/meetings/{meetingId}");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

            var response = await _httpClient.SendAsync(request);
            var responseContent = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                return new MeetingDetailsResult
                {
                    Success = false,
                    MeetingId = meetingId,
                    MeetingUri = string.Empty,
                    ErrorMessage = $"Failed to get meeting details: {response.StatusCode}"
                };
            }

            var meetingData = JsonSerializer.Deserialize<ZoomMeetingResponse>(responseContent, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (meetingData == null)
            {
                return new MeetingDetailsResult
                {
                    Success = false,
                    MeetingId = meetingId,
                    MeetingUri = string.Empty,
                    ErrorMessage = "Failed to parse meeting details"
                };
            }

            return new MeetingDetailsResult
            {
                Success = true,
                MeetingId = meetingData.Id.ToString(),
                MeetingUri = meetingData.JoinUrl ?? string.Empty,
                MeetingCode = meetingData.Id.ToString(),
                Status = meetingData.Status ?? "waiting"
            };
        }
        catch (Exception ex)
        {
            return new MeetingDetailsResult
            {
                Success = false,
                MeetingId = meetingId,
                MeetingUri = string.Empty,
                ErrorMessage = $"Exception: {ex.Message}"
            };
        }
    }

    private async Task<string> GetAccessTokenAsync()
    {
        // This should use Zoom's OAuth2 Server-to-Server authentication
        // For now, we'll retrieve from configuration
        // In production, implement proper OAuth2 flow
        
        var accessToken = _configuration["Zoom:AccessToken"];
        if (string.IsNullOrEmpty(accessToken))
        {
            throw new InvalidOperationException("Zoom access token not configured");
        }

        return accessToken;
    }

    private class ZoomMeetingResponse
    {
        public long Id { get; set; }
        public string? Topic { get; set; }
        public string? JoinUrl { get; set; }
        public string? StartUrl { get; set; }
        public string? Status { get; set; }
        public DateTime? StartTime { get; set; }
        public int Duration { get; set; }
    }
}