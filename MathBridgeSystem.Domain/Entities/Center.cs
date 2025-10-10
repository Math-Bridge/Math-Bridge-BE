using System;
using System.Collections.Generic;

namespace MathBridge.Domain.Entities;

public partial class Center
{
    public Guid CenterId { get; set; }

    public string Name { get; set; } = null!;

    public double? Latitude { get; set; }

    public double? Longitude { get; set; }

    public DateTime CreatedDate { get; set; }

    public DateTime? UpdatedDate { get; set; }

    public string? GooglePlaceId { get; set; }

    public string? FormattedAddress { get; set; }

    public string? City { get; set; }

    public string? District { get; set; }

    public string? PlaceName { get; set; }

    public string? CountryCode { get; set; }

    public DateTime? LocationUpdatedDate { get; set; }

    public int TutorCount { get; set; }

    public virtual ICollection<Child> Children { get; set; } = new List<Child>();

    public virtual ICollection<Contract> Contracts { get; set; } = new List<Contract>();

    public virtual ICollection<TutorCenter> TutorCenters { get; set; } = new List<TutorCenter>();
}
