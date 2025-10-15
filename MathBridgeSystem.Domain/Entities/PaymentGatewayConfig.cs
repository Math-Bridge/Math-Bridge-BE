using System;
using System.Collections.Generic;

namespace MathBridgeSystem.Domain.Entities;

public partial class PaymentGatewayConfig
{
    public int GatewayId { get; set; }

    public string GatewayName { get; set; } = null!;

    public bool IsEnabled { get; set; }

    public string DisplayName { get; set; } = null!;

    public int DisplayOrder { get; set; }

    public decimal MinAmount { get; set; }

    public decimal MaxAmount { get; set; }

    public string? Description { get; set; }

    public string? IconUrl { get; set; }

    public DateTime CreatedDate { get; set; }

    public DateTime? UpdatedDate { get; set; }
}
