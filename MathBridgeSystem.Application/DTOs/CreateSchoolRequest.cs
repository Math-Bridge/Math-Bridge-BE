using System;

namespace MathBridgeSystem.Application.DTOs
{
    public class CreateSchoolRequest
    {
        public string Name { get; set; } = string.Empty;
        public string PlaceId { get; set; } = string.Empty;
    }
}