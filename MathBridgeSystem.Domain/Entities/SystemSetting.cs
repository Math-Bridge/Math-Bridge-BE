using System;
using System.Collections.Generic;

namespace MathBridgeSystem.Domain.Entities;

public partial class SystemSetting
{
    public Guid Id { get; set; }

    public string Key { get; set; } = null!;

    public string Value { get; set; } = null!;

    public string? Description { get; set; }
}
