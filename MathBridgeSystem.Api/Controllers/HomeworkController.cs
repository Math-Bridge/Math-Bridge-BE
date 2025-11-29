using MathBridgeSystem.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MathBridgeSystem.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class HomeworkController : ControllerBase
{
    private readonly IHomeworkHelperService _homeworkHelperService;

    public HomeworkController(IHomeworkHelperService homeworkHelperService)
    {
        _homeworkHelperService = homeworkHelperService;
    }

    [HttpPost("analyze")]
    [Authorize(Roles = "parent")]
    public async Task<IActionResult> AnalyzeHomework(IFormFile file)
    {
        try
        {
            var result = await _homeworkHelperService.AnalyzeHomeworkAsync(file);
            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Internal server error: {ex.Message}");
        }
    }
}