using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using MathBridgeSystem.Application.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using System;
using System.Threading.Tasks;

namespace MathBridgeSystem.Infrastructure.Services;

public class CloudinaryService : ICloudinaryService
{
    private readonly Cloudinary _cloudinary;

    public CloudinaryService(IConfiguration configuration)
    {
        var account = new Account(
            configuration["Cloudinary:CloudName"],
            configuration["Cloudinary:ApiKey"],
            configuration["Cloudinary:ApiSecret"]
        );
        _cloudinary = new Cloudinary(account);
    }

    public async Task<string> UploadAvatarAsync(IFormFile file, Guid userId)
    {
        if (file == null || file.Length == 0)
            throw new ArgumentException("File is empty");

        // Validate file type
        var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".webp" };
        var fileExtension = System.IO.Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!System.Linq.Enumerable.Contains(allowedExtensions, fileExtension))
            throw new ArgumentException("Invalid file type. Only JPG, PNG, and WebP are allowed.");

        // Validate file size (5MB limit)
        if (file.Length > 5 * 1024 * 1024)
            throw new ArgumentException("File size exceeds 5MB limit.");

        using var stream = file.OpenReadStream();
        
        var uploadParams = new ImageUploadParams
        {
            File = new FileDescription(file.FileName, stream),
            PublicId = userId.ToString(),
            Overwrite = true,
            Transformation = new Transformation()
                .Width(500).Height(500).Crop("fill").Gravity("face")
                .FetchFormat("auto").Quality("auto:low")
        };

        var uploadResult = await _cloudinary.UploadAsync(uploadParams);

        if (uploadResult.Error != null)
        {
             throw new Exception($"Cloudinary upload failed: {uploadResult.Error.Message}");
        }

        return uploadResult.SecureUrl.ToString();
    }

    public async Task DeleteImageAsync(string publicId)
    {
        var deletionParams = new DeletionParams(publicId);
        var result = await _cloudinary.DestroyAsync(deletionParams);
        
        if (result.Result != "ok" && result.Result != "not found")
             throw new Exception($"Cloudinary deletion failed: {result.Result}");
    }
}