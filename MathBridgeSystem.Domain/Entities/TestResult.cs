using System;
using System.Collections.Generic;

namespace MathBridgeSystem.Domain.Entities;

public partial class TestResult
{
    public Guid ResultId { get; set; }

    public string TestType { get; set; } = null!;

    public decimal Score { get; set; }

    public string? Notes { get; set; }

    public DateTime CreatedDate { get; set; }

    public DateTime UpdatedDate { get; set; }

    public Guid ContractId { get; set; }

    public Guid? BookingId { get; set; }

    public virtual Session? Booking { get; set; }

    public virtual Contract Contract { get; set; } = null!;
}
