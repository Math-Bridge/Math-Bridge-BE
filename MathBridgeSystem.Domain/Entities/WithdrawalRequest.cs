using System;
using System.Collections.Generic;

namespace MathBridgeSystem.Domain.Entities;

public partial class WithdrawalRequest
{
    public Guid Id { get; set; }

    public Guid ParentId { get; set; }

    public Guid? StaffId { get; set; }

    public decimal Amount { get; set; }

    public DateTime CreatedDate { get; set; }

    public string BankName { get; set; } = null!;

    public string BankAccountNumber { get; set; } = null!;

    public string BankHolderName { get; set; } = null!;

    public string Status { get; set; } = null!;

    public DateTime? ProcessedDate { get; set; }

    public virtual User Parent { get; set; } = null!;

    public virtual User? Staff { get; set; }
}