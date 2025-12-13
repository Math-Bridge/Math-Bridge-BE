using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MathBridgeSystem.Application.DTOs.Withdrawal
{
    public class WithdrawalResponseDTO
    {
        public Guid Id { get; set; }
        public Guid ParentId { get; set; }
        public decimal Amount { get; set; }
        public string BankName { get; set; } = null!;
        public string BankAccountNumber { get; set; } = null!;
        public string BankHolderName { get; set; } = null!;
        public string Status { get; set; } = null!;
        public DateTime CreatedDate { get; set; }
        public DateTime? ProcessedDate { get; set; }
        public Guid? StaffId { get; set; }
    }
}
