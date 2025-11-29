using MathBridgeSystem.Application.DTOs;
using Microsoft.AspNetCore.Http;

namespace MathBridgeSystem.Application.Interfaces;

public interface IHomeworkHelperService
{
    Task<HomeworkAnalysisResult> AnalyzeHomeworkAsync(IFormFile file);
}