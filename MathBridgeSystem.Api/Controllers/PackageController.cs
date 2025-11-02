using MathBridgeSystem.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

[Route("api/packages")]
[ApiController]
public class PackageController : ControllerBase
{
    private readonly IPackageService _packageService;

    public PackageController(IPackageService packageService)
    {
        _packageService = packageService ?? throw new ArgumentNullException(nameof(packageService));
    }

    [HttpGet]
    public async Task<IActionResult> GetAllPackages()
    {
        try
        {
            var packages = await _packageService.GetAllPackagesAsync();
            return Ok(packages);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }
}