using MathBridge.Application.DTOs;
using MathBridge.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MathBridgeSystem.Api.Controllers
{
    [Route("api/admin/packages")]
    [ApiController]
    [Authorize(Roles = "admin")]
    public class AdminPackageController : ControllerBase
    {
        private readonly IPackageService _packageService;

        public AdminPackageController(IPackageService packageService)
        {
            _packageService = packageService ?? throw new ArgumentNullException(nameof(packageService));
        }

        [HttpPost]
        public async Task<IActionResult> CreatePackage([FromBody] CreatePackageRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                var packageId = await _packageService.CreatePackageAsync(request);
                return Ok(new { packageId });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }
    }

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
}

