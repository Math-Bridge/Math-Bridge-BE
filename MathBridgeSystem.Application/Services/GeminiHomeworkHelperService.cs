using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using MathBridgeSystem.Application.DTOs;
using MathBridgeSystem.Application.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Processing;

namespace MathBridgeSystem.Application.Services;

public class GeminiHomeworkHelperService : IHomeworkHelperService
{
    private readonly HttpClient _httpClient;
    private readonly string _apiKey;
    private const string GeminiApiUrl = "https://generativelanguage.googleapis.com/v1beta/models/gemini-2.5-flash-lite:generateContent";

    public GeminiHomeworkHelperService(HttpClient httpClient, IConfiguration configuration)
    {
        _httpClient = httpClient;
        _apiKey = configuration["Gemini:ApiKey"] ?? throw new InvalidOperationException("Gemini API Key is missing in configuration.");
    }

    public async Task<HomeworkAnalysisResult> AnalyzeHomeworkAsync(IFormFile file)
    {
        if (file == null || file.Length == 0)
        {
            throw new ArgumentException("File is empty or null.");
        }

        if (!file.ContentType.StartsWith("image/"))
        {
            throw new ArgumentException("Only image files are supported.");
        }

        string base64Image;
        using (var stream = file.OpenReadStream())
        using (var image = await Image.LoadAsync(stream))
        {
            if (image.Width > 1024)
            {
                image.Mutate(x => x.Resize(new ResizeOptions
                {
                    Mode = ResizeMode.Max,
                    Size = new Size(1024, 1024)
                }));
            }

            using (var memoryStream = new MemoryStream())
            {
                await image.SaveAsync(memoryStream, new JpegEncoder { Quality = 80 });
                base64Image = Convert.ToBase64String(memoryStream.ToArray());
            }
        }

        var promptText = "You are a helpful math teacher, answers ABSOLUTELY follow the instruction. Task: 1. Transcribe the math problem to LaTeX (DON'T PUT \\n  \\ OR ANY CHARACTERS/FORMAT BEFORE THE LATEX). 2. Provide short steps with proper format step by step instructions for students to follow to solve the problem. Output valid JSON with keys: latex, hint.";

        var requestPayload = new
        {
            contents = new[]
            {
                new
                {
                    parts = new object[]
                    {
                        new { text = promptText },
                        new
                        {
                            inline_data = new
                            {
                                mime_type = "image/jpeg",
                                data = base64Image
                            }
                        }
                    }
                }
            }
        };

        var response = await _httpClient.PostAsJsonAsync($"{GeminiApiUrl}?key={_apiKey}", requestPayload);
        response.EnsureSuccessStatusCode();

        var responseJson = await response.Content.ReadAsStringAsync();
        var geminiResponse = JsonSerializer.Deserialize<GeminiResponse>(responseJson);

        var textContent = geminiResponse?.Candidates?.FirstOrDefault()?.Content?.Parts?.FirstOrDefault()?.Text;

        if (string.IsNullOrEmpty(textContent))
        {
             throw new Exception("Failed to generate content from Gemini API.");
        }

        textContent = textContent.Trim();
        string tick = "`";
        string markdownJson = tick + tick + tick + "json";
        string markdownEnd = tick + tick + tick;

        if (textContent.StartsWith(markdownJson))
        {
            textContent = textContent.Substring(markdownJson.Length);
        }
        else if (textContent.StartsWith(markdownEnd))
        {
             textContent = textContent.Substring(markdownEnd.Length);
        }

        if (textContent.EndsWith(markdownEnd))
        {
            textContent = textContent.Substring(0, textContent.Length - markdownEnd.Length);
        }

        textContent = textContent.Trim();

        try
        {
            var result = JsonSerializer.Deserialize<HomeworkAnalysisResult>(textContent, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            return result ?? new HomeworkAnalysisResult();
        }
        catch (JsonException)
        {
            return new HomeworkAnalysisResult { Hint = textContent };
        }
    }

    private class GeminiResponse
    {
        [JsonPropertyName("candidates")]
        public List<Candidate>? Candidates { get; set; } = new();
    }

    private class Candidate
    {
        [JsonPropertyName("content")]
        public Content? Content { get; set; }
    }

    private class Content
    {
        [JsonPropertyName("parts")]
        public List<Part>? Parts { get; set; } = new();
    }

    private class Part
    {
        [JsonPropertyName("text")]
        public string? Text { get; set; }
    }
}