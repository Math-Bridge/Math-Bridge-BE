using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;

namespace MathBridgeSystem.Application.Interfaces;

public interface ICloudinaryService
{
    Task<string> UploadAvatarAsync(IFormFile file, Guid userId);
    Task DeleteImageAsync(string publicId);
}