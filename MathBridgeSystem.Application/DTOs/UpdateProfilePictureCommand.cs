using Microsoft.AspNetCore.Http;
using System;
using System.ComponentModel.DataAnnotations;

namespace MathBridgeSystem.Application.DTOs;

public class UpdateProfilePictureCommand
{
    [Required]
    public IFormFile File { get; set; }

    [Required]
    public Guid UserId { get; set; }
}