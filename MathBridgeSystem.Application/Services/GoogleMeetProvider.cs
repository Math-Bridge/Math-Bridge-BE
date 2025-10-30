using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using MathBridgeSystem.Application.Interfaces;
using Google.Apis.Auth.OAuth2;
using Google.Apps.Meet.V2;
using Grpc.Core;
using Google.Api.Gax.Grpc;
using Google.Apis.Util.Store;

namespace MathBridgeSystem.Application.Services;

public class GoogleMeetProvider : IVideoConferenceProvider
{
    private readonly IConfiguration _configuration;

    public string PlatformName => "GoogleMeet";

    public GoogleMeetProvider(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public async Task<VideoConferenceCreationResult> CreateMeetingAsync(
        string displayName, 
        DateTime startTime, 
        DateTime endTime)
    {
        try
        {
            var credentials = await GetCredentialsAsync();
            Console.WriteLine($"[DEBUG] CreateMeetingAsync: Credentials obtained");
            
            var builder = new SpacesServiceClientBuilder();
            builder.Credential = credentials;
            Console.WriteLine($"[DEBUG] CreateMeetingAsync: Builder credential set");
            
            var client = await builder.BuildAsync();
            Console.WriteLine($"[DEBUG] CreateMeetingAsync: Client built successfully");
            
            var space = new Space
            {
            };
            
            var request = new CreateSpaceRequest { Space = space };
            Console.WriteLine($"[DEBUG] CreateMeetingAsync: Sending CreateSpaceRequest for: {displayName}");
            var response = await client.CreateSpaceAsync(request);
            Console.WriteLine($"[DEBUG] CreateMeetingAsync: Response received: {response?.Name}");

            if (response == null || string.IsNullOrEmpty(response.Name))
            {
                return new VideoConferenceCreationResult
                {
                    Success = false,
                    ErrorMessage = "Failed to create Google Meet space: Invalid response"
                };
            }

            return new VideoConferenceCreationResult
            {
                Success = true,
                MeetingId = response.Name,
                SpaceName = displayName,
                MeetingUri = response.MeetingUri ?? string.Empty,
                MeetingCode = ExtractMeetingCode(response.MeetingUri)
            };
        }
        catch (RpcException ex) when (ex.StatusCode == StatusCode.PermissionDenied)
        {
            Console.WriteLine($"[ERROR] PermissionDenied: {ex.Message}");
            return new VideoConferenceCreationResult
            {
                Success = false,
                ErrorMessage = $"Google Meet API Permission Denied: Ensure service account has meetings.space.created scope and proper IAM permissions. Error: {ex.Message}"
            };
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ERROR] CreateMeetingAsync exception: {ex.GetType().Name} - {ex.Message}");
            Console.WriteLine($"[ERROR] Stack trace: {ex.StackTrace}");
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
            var credentials = await GetCredentialsAsync();
            
            var builder = new SpacesServiceClientBuilder();
            var client = await builder.BuildAsync();

            return true;
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
            var credentials = await GetCredentialsAsync();
            
            var builder = new SpacesServiceClientBuilder();
            var client = await builder.BuildAsync();

            var request = new GetSpaceRequest
            {
                SpaceName = SpaceName.FromSpace(meetingId.Replace("spaces/", ""))
            };

            var response = await client.GetSpaceAsync(request);

            if (response == null)
            {
                return new MeetingDetailsResult
                {
                    Success = false,
                    MeetingId = meetingId,
                    MeetingUri = string.Empty,
                    ErrorMessage = "Failed to retrieve meeting details"
                };
            }

            return new MeetingDetailsResult
            {
                Success = true,
                MeetingId = response.Name ?? meetingId,
                MeetingUri = response.MeetingUri ?? string.Empty,
                MeetingCode = ExtractMeetingCode(response.MeetingUri),
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

    private async Task<ICredential> GetCredentialsAsync()
    {
        try
        {
            var oauthJsonPath = _configuration["GoogleMeet:OAuthCredentialsPath"];
            Console.WriteLine($"[DEBUG] GoogleMeet:OAuthCredentialsPath = {oauthJsonPath}");
            
            if (string.IsNullOrEmpty(oauthJsonPath))
            {
                throw new InvalidOperationException("GoogleMeet:OAuthCredentialsPath configuration is missing");
            }

            if (!Path.IsPathRooted(oauthJsonPath))
            {
                oauthJsonPath = Path.Combine(Directory.GetCurrentDirectory(), oauthJsonPath);
                Console.WriteLine($"[DEBUG] Relative path converted to: {oauthJsonPath}");
            }

            Console.WriteLine($"[DEBUG] Checking if OAuth credentials file exists: {oauthJsonPath}");
            if (!File.Exists(oauthJsonPath))
            {
                throw new FileNotFoundException($"OAuth credentials file not found at: {oauthJsonPath}");
            }

            Console.WriteLine($"[DEBUG] OAuth credentials file found! Loading...");
            
            var scopes = new[]
            {
                "https://www.googleapis.com/auth/meetings.space.created",
                "https://www.googleapis.com/auth/calendar"
            };

            string credentialCachePath = Path.Combine(Directory.GetCurrentDirectory(), "GoogleMeetTokenCache");
            Directory.CreateDirectory(credentialCachePath);
            Console.WriteLine($"[DEBUG] Token cache path: {credentialCachePath}");

            var fileDataStore = new FileDataStore(credentialCachePath, true);
            
            GoogleClientSecrets clientSecrets;
            using (var stream = new FileStream(oauthJsonPath, FileMode.Open, FileAccess.Read))
            {
                clientSecrets = GoogleClientSecrets.FromStream(stream);
                Console.WriteLine($"[DEBUG] Client secrets loaded successfully");
            }

            var credential = await GoogleWebAuthorizationBroker.AuthorizeAsync(
                clientSecrets.Secrets,
                scopes,
                "mathbridge-user",
                CancellationToken.None,
                fileDataStore);

            Console.WriteLine($"[DEBUG] OAuth 2.0 authorization successful");
            return credential;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ERROR] Error obtaining Google Meet credentials: {ex.Message}");
            Console.WriteLine($"[ERROR] Stack trace: {ex.StackTrace}");
            throw new InvalidOperationException($"Error obtaining Google Meet credentials: {ex.Message}", ex);
        }
    }

    private string ExtractMeetingCode(string meetingUri)
    {
        if (string.IsNullOrEmpty(meetingUri))
            return string.Empty;
        
        try
        {
            var uri = new Uri(meetingUri);
            return uri.Segments.LastOrDefault()?.TrimEnd('/') ?? string.Empty;
        }
        catch
        {
            return string.Empty;
        }
    }
}
