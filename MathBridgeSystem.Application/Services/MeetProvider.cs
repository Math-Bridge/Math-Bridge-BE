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
using MathBridgeSystem.Application.DTOs.VideoConference;

namespace MathBridgeSystem.Application.Services;

public class MeetProvider : IVideoConferenceProvider
{
    private readonly IConfiguration _configuration;

    public string PlatformName => "Meet";

    public MeetProvider(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public async Task<VideoConferenceCreationResult> CreateMeetingAsync()
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
    
    public async Task<MeetingDetailsResult> GetMeetingDetailsAsync(string meetingId)
    {
        try
        {
            
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
        var userEmail = _configuration["GoogleMeet:WorkspaceUserEmail"];
        
        Console.WriteLine($"[DEBUG] GoogleMeet:OAuthCredentialsPath = {oauthJsonPath}");
        Console.WriteLine($"[DEBUG] GoogleMeet:WorkspaceUserEmail = {userEmail}");
        
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

        var scopes = new[]
        {
            "https://www.googleapis.com/auth/meetings.space.created",
            "https://www.googleapis.com/auth/calendar"
        };

        // Load the service account credential from the JSON file
        var googleCredential = GoogleCredential.FromFile(oauthJsonPath);
        
        // Extract the underlying service account credential
        var originalCredential = googleCredential.UnderlyingCredential as ServiceAccountCredential;
        
        if (originalCredential == null)
        {
            throw new InvalidOperationException("The credential file does not contain a service account credential");
        }

        // Create initializer with token server URL
        var initializer = new ServiceAccountCredential.Initializer(
            originalCredential.Id, 
            "https://oauth2.googleapis.com/token")  // ← Token server URL
        {
            User = userEmail,  // ← Domain-wide delegation
            Key = originalCredential.Key,
            KeyId = originalCredential.KeyId,
            Scopes = scopes
        };

        // Create the delegated credential
        var delegatedCredential = new ServiceAccountCredential(initializer);

        Console.WriteLine($"[DEBUG] Server-to-server OAuth with delegation to {userEmail} successful");
        return delegatedCredential;
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
