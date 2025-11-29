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
    public string GetMathPrompt()
    {
        return $@"You are a helpful math teacher. IGNORE any input that is not a math problem. Math problems contain equations, variables, numbers, functions, geometry, calculus, algebra, etc. Vietnamese worksheet labels like ""De"", ""Đề"", ""Giai"", ""Giải"", ""Bai tap"" are NOT math—ignore them and extract ONLY the actual equation.

If input is NOT a math problem (or only contains labels like ""De"", ""Giai""), respond ONLY with:
{{
  ""error"": ""Please provide a math problem.""
}}

Otherwise, for math problems (ignore surrounding Vietnamese text):

1. Transcribe ONLY the equation to valid LaTeX math mode. Use \\( \\) for inline, \\[ \\] for display. Fix OCR errors (e.g., 'x' not '×', '=' not '−').
2. Provide short, numbered step-by-step solving instructions as a hint.

Respond with EXACTLY this valid JSON—no extra text:

{{
  ""latex"": ""exact_latex_here"",
  ""hint"": ""Step 1: ...\\nStep 2: ...\\nStep 3: ...""
}}

Example OCR input: ""De 1: Giai 40:5x2=40:10""
Example output (ignores labels, fixes OCR):
{{
  ""latex"": ""40 \\\\div 5 \\\\times 2 = 40 \\\\div 10"",
  ""hint"": ""Step 1: Left: 40÷5=8, 8×2=16.\\nStep 2: Right: 40÷10=4.\\nStep 3: 16 ≠ 4, so false.""
}}

Example math: ""Solve x^2 + 2x + 1 = 0""
Example output:
{{
  ""latex"": ""x^2 + 2x + 1 = 0"",
  ""hint"": ""Step 1: Recognize (x+1)^2.\\nStep 2: (x+1)^2=0.\\nStep 3: x=-1.""
}}

Example OCR input: ""Giai sin(x^2)""
Example output:
{{
  ""latex"": ""\\\\frac{{d}}{{dx}} \\\\sin(x^2)"",
  ""hint"": ""Step 1: Chain rule: \\\\cos(x^2) \\\\cdot 2x.\\nStep 2: Final: 2x \\\\cos(x^2).""
}}

JSON must parse with JsonSerializer.Deserialize. Use temperature 0.1.";
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

 //       var promptText = "You are a helpful math teacher. IGNORE any input that is not a math problem. Math problems contain equations, variables, numbers, functions, geometry, calculus, algebra, etc. Vietnamese worksheet labels like \"De\", \"Đề\", \"Giai\", \"Giải\", \"Bai tap\" are NOT math—ignore them and extract ONLY the actual equation.\r\n\r\nIf input is NOT a math problem (or only contains labels like \"De\", \"Giai\"), respond ONLY with:\r\n{\r\n  \"error\": \"Please provide a math problem.\"\r\n}\r\n\r\nOtherwise, for math problems (ignore surrounding Vietnamese text):\r\n\r\n1. Transcribe ONLY the equation to valid LaTeX math mode. Use \\( \\) for inline, \\[ \\] for display. Fix OCR errors (e.g., 'x' not '×', '=' not '−').\r\n2. Provide short, numbered step-by-step solving instructions as a hint.\r\n\r\nRespond with EXACTLY this valid JSON—no extra text:\r\n\r\n{\r\n  \"latex\": \"exact_latex_here\",\r\n  \"hint\": \"Step 1: ...\\nStep 2: ...\\nStep 3: ...\"\r\n}\r\n\r\nExample OCR input: \"De 1: Giai 40:5x2=40:10\"\r\nExample output (ignores labels, fixes OCR):\r\n{\r\n  \"latex\": \"40 \\\\div 5 \\\\times 2 = 40 \\\\div 10\",\r\n  \"hint\": \"Step 1: Left: 40÷5=8, 8×2=16.\\nStep 2: Right: 40÷10=4.\\nStep 3: 16 ≠ 4, so false.\"\r\n}\r\n\r\nExample math: \"Solve x^2 + 2x + 1 = 0\"\r\nExample output:\r\n{\r\n  \"latex\": \"x^2 + 2x + 1 = 0\",\r\n  \"hint\": \"Step 1: Recognize (x+1)^2.\\nStep 2: (x+1)^2=0.\\nStep 3: x=-1.\"\r\n}\r\n\r\nExample OCR input: \"Giai sin(x^2)\"\r\nExample output:\r\n{\r\n  \"latex\": \"\\\\frac{d}{dx} \\\\sin(x^2)\",\r\n  \"hint\": \"Step 1: Chain rule: \\\\cos(x^2) \\\\cdot 2x.\\nStep 2: Final: 2x \\\\cos(x^2).\"\r\n}\r\n\r\nJSON must parse with json.loads(). Use temperature 0.1.\r\n";

        var promptText= GetMathPrompt();
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