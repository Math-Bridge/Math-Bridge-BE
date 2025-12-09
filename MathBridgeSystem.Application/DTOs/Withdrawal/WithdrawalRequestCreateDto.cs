using System.ComponentModel.DataAnnotations;

namespace MathBridgeSystem.Application.DTOs.Withdrawal;

public class WithdrawalRequestCreateDto
{
    [Required]
    [Range(0.01, double.MaxValue, ErrorMessage = "Amount must be greater than 0")]
    public decimal Amount { get; set; }

    [Required(ErrorMessage = "Bank Name is required")]
    public string BankName { get; set; } = null!;

    [Required(ErrorMessage = "Bank Account Number is required")]
    public string BankAccountNumber { get; set; } = null!;

    [Required(ErrorMessage = "Bank Account Holder Name is required")]
    public string BankHolderName { get; set; } = null!;
}