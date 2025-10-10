using System;
using System.Collections.Generic;

namespace MathBridge.Domain.Entities;

public partial class TutorCenter
{
    public Guid TutorCenterId { get; set; }

    public Guid TutorId { get; set; }

    public Guid CenterId { get; set; }

    public DateTime CreatedDate { get; set; }

    public virtual Center Center { get; set; } = null!;

    public virtual User Tutor { get; set; } = null!;
}
