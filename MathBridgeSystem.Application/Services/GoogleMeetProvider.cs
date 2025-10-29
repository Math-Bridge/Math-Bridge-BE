using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using MathBridgeSystem.Application.Interfaces;
using Google.Apis.Auth.OAuth2;

namespace MathBridgeSystem.Infrastructure.Services;

public class GoogleMeetProvider : IVideoConferenceProvider
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly string _apiBaseUrl = "https://meet.googleapis.com/v2";

    public string PlatformName => "GoogleMeet";

    public GoogleMeetProvider(HttpClient httpClient, IConfiguration configuration)
    {
        _httpClient = httpClient;
        _configuration = configuration;
    }

    public async Task<VideoConferenceCreationResult> CreateMeetingAsync(
        string displayName, 
        DateTime startTime, 
        DateTime endTime)
    {
        try
        {
            var accessToken = await GetAccessTokenAsync();
            
            // Create a new space (conference)
            var requestBody = new
            {
                config = new
                {
                    entryPointAccess = "ALL",
                    accessType = "OPEN"
                }
            };

            var request = new HttpRequestMessage(HttpMethod.Post, $"{_apiBaseUrl}/spaces");
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
                    ErrorMessage = $"Google Meet API error: {response.StatusCode} - {responseContent}"
                };
            }

            var spaceData = JsonSerializer.Deserialize<GoogleMeetSpaceResponse>(responseContent, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (spaceData == null || string.IsNullOrEmpty(spaceData.Name))
            {
                return new VideoConferenceCreationResult
                {
                    Success = false,
                    ErrorMessage = "Failed to parse Google Meet response"
                };
            }

            // Extract meeting code from the meetingUri if available
            string? meetingCode = null;
            if (!string.IsNullOrEmpty(spaceData.MeetingUri))
            {
                var uri = new Uri(spaceData.MeetingUri);
                meetingCode = uri.Segments.LastOrDefault()?.TrimEnd('/');
            }

            return new VideoConferenceCreationResult
            {
                Success = true,
                MeetingId = spaceData.Name,
                SpaceName = displayName,
                MeetingUri = spaceData.MeetingUri ?? $"https://meet.google.com/{meetingCode}",
                MeetingCode = meetingCode
            };
        }
        catch (Exception ex)
        {
            return new VideoConferenceCreationResult
            {
                Success = false,
                ErrorMessage = $"Exception creating Google Meet: {ex.Message}"
            };
        }
    }

    public async Task<bool> DeleteMeetingAsync(string meetingId)
    {
        try
        {
            var accessToken = await GetAccessTokenAsync();

            var request = new HttpRequestMessage(HttpMethod.Delete, $"{_apiBaseUrl}/{meetingId}");
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

            var request = new HttpRequestMessage(HttpMethod.Get, $"{_apiBaseUrl}/{meetingId}");
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

            var spaceData = JsonSerializer.Deserialize<GoogleMeetSpaceResponse>(responseContent, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (spaceData == null)
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
                MeetingId = spaceData.Name ?? meetingId,
                MeetingUri = spaceData.MeetingUri ?? string.Empty,
                MeetingCode = spaceData.MeetingCode,
                Status = "Active"
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
        try
        {
            var serviceAccountPath = _configuration["GoogleMeet:ServiceAccountJsonPath"];
            if (string.IsNullOrEmpty(serviceAccountPath))
            {
                throw new InvalidOperationException("Google Meet service account path not configured. Please add 'GoogleMeet:ServiceAccountJsonPath' to appsettings.json");
            }

            // Ensure the path is absolute
            if (!Path.IsPathRooted(serviceAccountPath))
            {
                serviceAccountPath = Path.Combine(Directory.GetCurrentDirectory(), serviceAccountPath);
            }

            if (!File.Exists(serviceAccountPath))
            {
                throw new FileNotFoundException($"Service account file not found at: {serviceAccountPath}");
            }

            // Load service account credentials
            GoogleCredential credential;
            using (var stream = new FileStream(serviceAccountPath, FileMode.Open, FileAccess.Read))
            {
                credential = GoogleCredential.FromStream(stream)
                    .CreateScoped(new[]
                    {
                        "https://www.googleapis.com/auth/meetings.space.created",
                        "https://www.googleapis.com/auth/meetings.space.readonly"
                    });
            }

            // Get the access token
            var token = await credential.UnderlyingCredential.GetAccessTokenForRequestAsync();
            if (string.IsNullOrEmpty(token))
            {
                throw new InvalidOperationException("Failed to obtain access token from service account");
            }

            return token;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Error obtaining Google Meet access token: {ex.Message}", ex);
        }
    }

    private class GoogleMeetSpaceResponse
    {
        public string? Name { get; set; }
        public string? MeetingUri { get; set; }
        public string? MeetingCode { get; set; }
    }
}