using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using MathBridgeSystem.Application.DTOs.VideoConference;
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
            //var userId = new HttpRequestMessage(HttpMethod.Get, $"{_apiBaseUrl}/users/{_configuration["Zoom:UserId"]}");
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

    

    private async Task<string> GetAccessTokenAsync()
{
    var clientId = _configuration["Zoom:ClientId"];
    var clientSecret = _configuration["Zoom:ClientSecret"];

    if (string.IsNullOrEmpty(clientId) || string.IsNullOrEmpty(clientSecret))
    {
        throw new InvalidOperationException("Zoom client credentials not configured");
    }

    var request = new HttpRequestMessage(HttpMethod.Post, "https://zoom.us/oauth/token");

    var authHeader = Convert.ToBase64String(System.Text.Encoding.ASCII.GetBytes($"{clientId}:{clientSecret}"));
    request.Headers.Authorization = new AuthenticationHeaderValue("Basic", authHeader);

    var content = new FormUrlEncodedContent(new[]
    {
        new KeyValuePair<string, string>("grant_type", "client_credentials")
    });
    request.Content = content;

    var response = await _httpClient.SendAsync(request);
    var responseContent = await response.Content.ReadAsStringAsync();
    if (!response.IsSuccessStatusCode)
    {
        throw new InvalidOperationException($"Failed to get Zoom access token: {response.StatusCode} - {responseContent}");
    }

    var json = await System.Text.Json.JsonDocument.ParseAsync(new System.IO.MemoryStream(System.Text.Encoding.UTF8.GetBytes(responseContent)));
    var token = json.RootElement.GetProperty("access_token").GetString();
    if (string.IsNullOrEmpty(token))
    {
        throw new InvalidOperationException("Zoom access token is empty or not found in response");
    }
    return token;
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