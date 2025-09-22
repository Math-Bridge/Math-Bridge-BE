using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MathBridge.Application.DTOs
{
    public class GeocodeResponse
    {
        public bool Success { get; set; }
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }
        public string? FormattedAddress { get; set; }
        public string? City { get; set; }
        public string? District { get; set; }
        public string? ErrorMessage { get; set; }

        public bool HasValidCoordinates => Success && Latitude.HasValue && Longitude.HasValue;
    }
}
