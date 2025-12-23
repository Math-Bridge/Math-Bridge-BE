using System.Globalization;
using MathBridgeSystem.Application.DTOs.SePay;
using MathBridgeSystem.Application.DTOs.Notification;
using MathBridgeSystem.Application.Interfaces;
using MathBridgeSystem.Domain.Entities;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Text.RegularExpressions;
using System.Web;
using MathBridgeSystem.Domain.Interfaces;

namespace MathBridgeSystem.Application.Services;

/// <summary>
/// Service implementation for SePay payment gateway operations
/// </summary>
public class SePayService : ISePayService
{
    private readonly ISePayRepository _sePayRepository;
    private readonly IWalletTransactionRepository _walletTransactionRepository;
    private readonly IUserRepository _userRepository;
    private readonly IContractRepository _contractRepository;
    private readonly IPackageRepository _packageRepository;
    private readonly IConfiguration _configuration;
    private readonly ILogger<SePayService> _logger;
    private readonly IEmailService _emailService;
    private readonly INotificationService _notificationService;

    // SePay configuration keys
    private readonly string _bankCode;
    private readonly string _accountNumber;
    private readonly string _accountName;
    private readonly string _qrBaseUrl;
    private readonly string _orderReferencePrefix;

    public SePayService(
        ISePayRepository sePayRepository,
        IWalletTransactionRepository walletTransactionRepository,
        IUserRepository userRepository,
        IContractRepository contractRepository,
        IPackageRepository packageRepository,
        IConfiguration configuration,
        ILogger<SePayService> logger,
        IEmailService emailService,
        INotificationService notificationService)
    {
        _sePayRepository = sePayRepository;
        _walletTransactionRepository = walletTransactionRepository;
        _userRepository = userRepository;
        _contractRepository = contractRepository;
        _packageRepository = packageRepository;
        _configuration = configuration;
        _logger = logger;
        _emailService = emailService;
        _notificationService = notificationService;

        // Load configuration
        _bankCode = _configuration["SePay:BankCode"]?? "";
        _accountNumber = _configuration["SePay:AccountNumber"] ?? "";
        _accountName = _configuration["SePay:AccountName"] ?? "";
        _qrBaseUrl = _configuration["SePay:QrBaseUrl"] ?? "https://qr.sepay.vn/img";
        _orderReferencePrefix = _configuration["SePay:OrderReferencePrefix"] ?? "MB";
    }

    public async Task<SePayPaymentResponseDto> CreatePaymentRequestAsync(SePayPaymentRequestDto request)
    {
        try
        {
            _logger.LogInformation("Creating SePay payment request for user {UserId}, amount {Amount}", 
                request.UserId, request.Amount);

            // Validate request
            if (request.Amount <= 0)
            {
                return new SePayPaymentResponseDto
                {
                    Success = false,
                    Message = "Amount must be greater than 0"
                };
            }

            // Check if user exists
            var user = await _userRepository.GetByIdAsync(request.UserId);
            if (user == null)
            {
                return new SePayPaymentResponseDto
                {
                    Success = false,
                    Message = "User not found"
                };
            }

            // Create wallet transaction
            var walletTransaction = new WalletTransaction
            {
                TransactionId = Guid.NewGuid(),
                ParentId = request.UserId,
                Amount = request.Amount,
                TransactionType = "Deposit",
                Description = $"SePay deposit: {request.Description}",
                TransactionDate = DateTime.UtcNow.ToLocalTime(),
                Status = "Pending",
                PaymentMethod = "SePay",
                PaymentGatewayReference = ""
            };

            var createdWalletTransaction = await _walletTransactionRepository.AddAsync(walletTransaction);

            // Generate order reference
            var orderReference = $"{_orderReferencePrefix}{createdWalletTransaction.TransactionId.ToString("N")[..8].ToUpper()}";

            // Generate QR code URL
            var qrCodeUrl = GenerateQrCodeUrl(request.Amount, orderReference);

            // Create SePay transaction record
            var sePayTransaction = new SepayTransaction
            {
                SepayTransactionId = Guid.NewGuid(),
                WalletTransactionId = createdWalletTransaction.TransactionId,
                OrderReference = orderReference,
                TransferAmount = request.Amount,
                TransferType = "in",
                Code = orderReference,
                Content = orderReference + "-" + request.Description,
                Description = request.Description,
                CreatedAt = DateTime.UtcNow.ToLocalTime()
            };

            await _sePayRepository.AddAsync(sePayTransaction);

            // Update wallet transaction with payment gateway reference
            createdWalletTransaction.PaymentGatewayReference = orderReference;
            await _walletTransactionRepository.UpdateAsync(createdWalletTransaction);

            return new SePayPaymentResponseDto
            {
                Success = true,
                Message = "Payment request created successfully",
                QrCodeUrl = qrCodeUrl,
                OrderReference = orderReference,
                WalletTransactionId = createdWalletTransaction.TransactionId,
                Amount = request.Amount,
                BankInfo = $"{_accountName} - {_accountNumber} - {_bankCode}",
                TransferContent = orderReference
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating SePay payment request for user {UserId}", request.UserId);
            return new SePayPaymentResponseDto
            {
                Success = false,
                Message = "An error occurred while creating payment request"
            };
        }
    }

    public async Task<SePayWebhookResultDto> ProcessWebhookAsync(SePayWebhookRequestDto webhookData)
    {
        try
        {
            _logger.LogInformation("Processing SePay webhook for transaction {Code}", webhookData.Code);

            // Check for contract payment
            if (!string.IsNullOrEmpty(webhookData.Code))
            {
                var contractTransaction = await _sePayRepository.GetByCodeAsync(webhookData.Code);
                if (contractTransaction?.ContractId != null && contractTransaction.ContractId != Guid.Empty)
                {
                    return await ProcessContractPaymentWebhookAsync(contractTransaction.ContractId, webhookData);
                }
            }

            // Check if we've already processed this transaction
            var existingTransaction = await _sePayRepository.ExistsByCodeAsync(webhookData.Code);
            if (existingTransaction == null)
            {
                _logger.LogWarning("No Sepay transaction with {Code} found", webhookData.Code);
                return new SePayWebhookResultDto
                {
                    Success = false,
                    Message = "No transaction found"
                };
            }
            if (existingTransaction.WalletTransaction.Status != "Pending")
            {
                _logger.LogWarning("SePay transaction {Code} already processed", webhookData.Code);
                return new SePayWebhookResultDto
                {
                    Success = true,
                    Message = "Transaction already processed"
                };
            }


            // Extract order reference from content
            var orderReference = ExtractOrderReference(webhookData.Content);
            _logger.LogInformation("Extracted order reference: '{OrderReference}' from content: '{Content}'", 
                orderReference ?? "NULL", webhookData.Content);
            
            if (string.IsNullOrEmpty(orderReference))
            {
                _logger.LogWarning("Could not extract order reference from content: {Content}", webhookData.Content);
                return new SePayWebhookResultDto
                {
                    Success = false,
                    Message = "Order reference not found in transaction content"
                };
            }

            // Find the corresponding wallet transaction
            _logger.LogInformation("Looking up transaction for order reference: {OrderReference}", orderReference);
            var sePayTransaction = await _sePayRepository.GetByOrderReferenceAsync(orderReference);
            
            if (sePayTransaction?.WalletTransaction == null)
            {
                _logger.LogWarning("Wallet transaction not found for order reference: {OrderReference}. " +
                    "This means either: 1) No payment was created with this reference, or " +
                    "2) The reference format doesn't match the expected pattern", orderReference);
                return new SePayWebhookResultDto
                {
                    Success = false,
                    Message = "Wallet transaction not found for order reference"
                };
            }
            
            _logger.LogInformation("Found transaction for order reference: {OrderReference}, " +
                "WalletTransactionId: {WalletTransactionId}, Status: {Status}", 
                orderReference, sePayTransaction.WalletTransactionId, sePayTransaction.WalletTransaction.Status);

            var walletTransaction = sePayTransaction.WalletTransaction;

            // Verify amount matches
            if (webhookData.TransferAmount != walletTransaction.Amount)
            {
                _logger.LogWarning("Amount mismatch: webhook {WebhookAmount}, wallet {WalletAmount}", 
                    webhookData.TransferAmount, walletTransaction.Amount);
                return new SePayWebhookResultDto
                {
                    Success = false,
                    Message = "Transaction amount does not match"
                };
            }

            // Verify transaction is still pending
            if (walletTransaction.Status != "Pending")
            {
                _logger.LogWarning("Wallet transaction {TransactionId} is not in pending status: {Status}", 
                    walletTransaction.TransactionId, walletTransaction.Status);
                return new SePayWebhookResultDto
                {
                    Success = false,
                    Message = "Transaction is not in pending status"
                };
            }

            // Update SePay transaction with webhook data
            sePayTransaction.Gateway = webhookData.Gateway;
            sePayTransaction.TransactionDate = webhookData.TransactionDate;
            sePayTransaction.AccountNumber = webhookData.AccountNumber;
            sePayTransaction.SubAccount = webhookData.SubAccount;
            sePayTransaction.TransferType = webhookData.TransferType;
            sePayTransaction.Accumulated = webhookData.Accumulated;
            sePayTransaction.Code = webhookData.Code;
            sePayTransaction.Content = webhookData.Content;
            sePayTransaction.ReferenceNumber = webhookData.ReferenceCode;
            sePayTransaction.Description = webhookData.Description;

            await _sePayRepository.UpdateAsync(sePayTransaction);

            // Update wallet transaction status to completed
            walletTransaction.Status = "Completed";
            await _walletTransactionRepository.UpdateAsync(walletTransaction);

            // Update user wallet balance
            var user = await _userRepository.GetByIdAsync(walletTransaction.ParentId);
            if (user != null)
            {
                user.WalletBalance += walletTransaction.Amount;
                await _userRepository.UpdateAsync(user);

                _logger.LogInformation("Successfully processed SePay webhook for transaction {Code}, updated wallet balance for user {UserId}", 
                    webhookData.Code, walletTransaction.ParentId);

                // Send email notification for wallet top-up
                try
                {
                    await _emailService.SendInvoiceAsync(
                        user.Email,
                        user.FullName,
                        orderReference,
                        walletTransaction.Amount.ToString("C", CultureInfo.GetCultureInfo("vi-VN")),
                        webhookData.TransactionDate.ToString("yyyy-MM-dd"),
                        $"https://mathbridge.com/transactions/{walletTransaction.TransactionId}"
                    );
                    _logger.LogInformation("Wallet top-up email sent to {Email} for transaction {Code}", user.Email, webhookData.Code);
                }
                catch (Exception emailEx)
                {
                    _logger.LogError(emailEx, "Failed to send wallet top-up email for transaction {Code}", webhookData.Code);
                }

                // Create in-app notification for wallet top-up
                try
                {
                    await _notificationService.CreateNotificationAsync(new CreateNotificationRequest
                    {
                        UserId = walletTransaction.ParentId,
                        Title = "Wallet Top-Up Successful",
                        Message = $"Your wallet has been topped up with {walletTransaction.Amount.ToString("C", CultureInfo.GetCultureInfo("vi-VN"))}. Transaction ID: {orderReference}",
                        NotificationType = "WalletTopUp"
                    });
                    _logger.LogInformation("Wallet top-up notification created for user {UserId}", walletTransaction.ParentId);
                }
                catch (Exception notificationEx)
                {
                    _logger.LogError(notificationEx, "Failed to create wallet top-up notification for transaction {Code}", webhookData.Code);
                }
            }

            return new SePayWebhookResultDto
            {
                Success = true,
                Message = "Webhook processed successfully",
                WalletTransactionId = walletTransaction.TransactionId,
                OrderReference = orderReference
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing SePay webhook for transaction {Code}", webhookData.Code);
            return new SePayWebhookResultDto
            {
                Success = false,
                Message = "An error occurred while processing webhook"
            };
        }
    }

    public async Task<PaymentStatusDto> CheckPaymentStatusAsync(Guid walletTransactionId)
    {
        try
        {
            var walletTransaction = await _walletTransactionRepository.GetByIdAsync(walletTransactionId);
            if (walletTransaction == null)
            {
                return new PaymentStatusDto
                {
                    Success = false,
                    Status = "NotFound",
                    Message = "Transaction not found"
                };
            }

            var sePayTransaction = await _sePayRepository.GetByWalletTransactionIdAsync(walletTransactionId);
            
            return new PaymentStatusDto
            {
                Success = true,
                Status = walletTransaction.Status == "Completed" ? "Paid" : 
                         walletTransaction.Status == "Pending" ? "Unpaid" : walletTransaction.Status,
                Message = $"Transaction status: {walletTransaction.Status}",
                PaidAt = walletTransaction.Status == "Completed" ? sePayTransaction?.TransactionDate : null,
                AmountPaid = walletTransaction.Status == "Completed" ? walletTransaction.Amount : null
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking payment status for transaction {TransactionId}", walletTransactionId);
            return new PaymentStatusDto
            {
                Success = false,
                Status = "Error",
                Message = "An error occurred while checking payment status"
            };
        }
    }

    public async Task<SePayPaymentResponseDto> CreateContractDirectPaymentAsync(Guid contractId, Guid userid, decimal amount)
    {
        try
        {
            // Validate amount
            if (amount <= 0)
            {
                _logger.LogWarning("Invalid amount {Amount} for contract payment {ContractId}", amount, contractId);
                return new SePayPaymentResponseDto
                {
                    Success = false,
                    Message = "Amount must be greater than 0"
                };
            }

            // Validate contract exists and user is owner
            var contract = await _contractRepository.GetByIdAsync(contractId);
            
            if (contract == null)
            {
                _logger.LogWarning("Contract not found: {ContractId}", contractId);
                return new SePayPaymentResponseDto
                {
                    Success = false,
                    Message = "Contract not found"
                };
            }

            _logger.LogInformation("Creating direct contract payment for contract {ContractId}, user {UserId}, amount {Amount}", 
                contractId, contract.ParentId, amount);

            if (contract.ParentId != userid)
            {
                _logger.LogWarning("User {UserId} is not authorized for contract {ContractId}", userid, contractId);
                return new SePayPaymentResponseDto
                {
                    Success = false,
                    Message = "User is not authorized for this contract"
                };
            }

            contract.Status = "unpaid";
            await _contractRepository.UpdateAsync(contract);

            // Generate order reference
            var orderReference = $"{_orderReferencePrefix}{Guid.NewGuid().ToString("N")[..8].ToUpper()}";

            // Create SepayTransaction with ContractId (NO WalletTransaction)
            var sePayTransaction = new SepayTransaction
            {
                SepayTransactionId = Guid.NewGuid(),
                ContractId = contractId,
                OrderReference = orderReference,
                TransferAmount = amount,
                TransferType = "in",
                Code = orderReference,
                Content = orderReference + "-Contract Payment",
                Description = $"Direct payment for contract {contractId}",
                CreatedAt = DateTime.UtcNow.ToLocalTime()
            };

            await _sePayRepository.AddAsync(sePayTransaction);

            // Generate QR code for payment
            var qrCodeUrl = GenerateQrCodeUrl(amount, orderReference);

            _logger.LogInformation("Created direct contract payment transaction {SepayTransactionId} for contract {ContractId}, amount {Amount}", 
                sePayTransaction.SepayTransactionId, contractId, amount);

            return new SePayPaymentResponseDto
            {
                Success = true,
                Message = "Contract payment request created successfully",
                QrCodeUrl = qrCodeUrl,
                OrderReference = orderReference,
                Amount = amount,
                BankInfo = $"{_accountName} - {_accountNumber} - {_bankCode}",
                TransferContent = orderReference
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating direct contract payment for contract {ContractId}, amount {Amount}", contractId, amount);
            return new SePayPaymentResponseDto
            {
                Success = false,
                Message = "An error occurred while creating payment request"
            };
        }
    }

    public async Task<SePayPaymentResponseDto?> GetPaymentDetailsAsync(Guid walletTransactionId)
    {
        try
        {
            var walletTransaction = await _walletTransactionRepository.GetByIdAsync(walletTransactionId);
            if (walletTransaction == null) return null;

            var sePayTransaction = await _sePayRepository.GetByWalletTransactionIdAsync(walletTransactionId);
            if (sePayTransaction == null) return null;

            var qrCodeUrl = GenerateQrCodeUrl(walletTransaction.Amount, sePayTransaction.OrderReference ?? "");

            return new SePayPaymentResponseDto
            {
                Success = true,
                QrCodeUrl = qrCodeUrl,
                OrderReference = sePayTransaction.OrderReference ?? "",
                WalletTransactionId = walletTransactionId,
                Amount = walletTransaction.Amount,
                BankInfo = $"{_accountName} - {_accountNumber} - {_bankCode}",
                TransferContent = sePayTransaction.OrderReference ?? "",
                Message = "Payment details retrieved successfully"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting payment details for transaction {TransactionId}", walletTransactionId);
            return null;
        }
    }

    private async Task<SePayWebhookResultDto> ProcessContractPaymentWebhookAsync(Guid? contractId, SePayWebhookRequestDto webhookData)
    {
        try
        {
            _logger.LogInformation("Processing contract payment webhook for contract {ContractId}", contractId);
            if (contractId == null || contractId == Guid.Empty)
            {
                _logger.LogWarning("Invalid contract ID in webhook data");
                return new SePayWebhookResultDto
                {
                    Success = false,
                    Message = "Invalid contract ID"
                };
            }
            Guid cid = contractId.Value;
            // Validate contract exists
            var contract = await _contractRepository.GetByIdAsync(cid);
            if (contract == null)
            {
                _logger.LogWarning("Contract not found: {ContractId}", contractId);
                return new SePayWebhookResultDto
                {
                    Success = false,
                    Message = "Contract not found"
                };
            }

            // Get SePay transaction
            var sePayTransaction = await _sePayRepository.GetByCodeAsync(webhookData.Code);
            if (sePayTransaction == null)
            {
                _logger.LogWarning("SePay transaction not found for code: {Code}", webhookData.Code);
                return new SePayWebhookResultDto
                {
                    Success = false,
                    Message = "Transaction not found"
                };
            }

            // Verify amount matches
            if (webhookData.TransferAmount != sePayTransaction.TransferAmount)
            {
                _logger.LogWarning("Amount mismatch for contract {ContractId}: webhook {WebhookAmount}, transaction {TransactionAmount}", 
                    contractId, webhookData.TransferAmount, sePayTransaction.TransferAmount);
                return new SePayWebhookResultDto
                {
                    Success = false,
                    Message = "Transaction amount does not match"
                };
            }

            // Update SepayTransaction with webhook data
            sePayTransaction.Gateway = webhookData.Gateway;
            sePayTransaction.TransactionDate = webhookData.TransactionDate;
            sePayTransaction.AccountNumber = webhookData.AccountNumber;
            sePayTransaction.SubAccount = webhookData.SubAccount;
            sePayTransaction.TransferType = webhookData.TransferType;
            sePayTransaction.Accumulated = webhookData.Accumulated;
            sePayTransaction.Code = webhookData.Code;
            sePayTransaction.Content = webhookData.Content;
            sePayTransaction.ReferenceNumber = webhookData.ReferenceCode;
            sePayTransaction.Description = webhookData.Description;

            await _sePayRepository.UpdateAsync(sePayTransaction);

            // Update contract status to Active
            contract.Status = "Pending";
            await _contractRepository.UpdateAsync(contract);

            _logger.LogInformation("Successfully processed contract payment webhook for contract {ContractId}, updated status to Active", contractId);

            // Get parent user for sending email/notification
            var parent = await _userRepository.GetByIdAsync(contract.ParentId);
            if (parent != null)
            {
                // Send email notification for contract payment
                try
                {
                    await _emailService.SendInvoiceAsync(
                        parent.Email,
                        parent.FullName,
                        sePayTransaction.OrderReference ?? "",
                        sePayTransaction.TransferAmount.ToString("C"),
                        webhookData.TransactionDate.ToString("yyyy-MM-dd"),
                        $"https://mathbridge.com/contracts/{contractId}"
                    );
                    _logger.LogInformation("Contract payment email sent to {Email} for contract {ContractId}", parent.Email, contractId);
                }
                catch (Exception emailEx)
                {
                    _logger.LogError(emailEx, "Failed to send contract payment email for contract {ContractId}", contractId);
                }

                // Create in-app notification for contract payment
                try
                {
                    await _notificationService.CreateNotificationAsync(new CreateNotificationRequest
                    {
                        UserId = contract.ParentId,
                        ContractId = contractId,
                        Title = "Contract Payment Successful",
                        Message = $"Payment of {sePayTransaction.TransferAmount:C} for contract has been received. Your contract is now pending approval. Order Reference: {sePayTransaction.OrderReference}",
                        NotificationType = "ContractPayment"
                    });
                    _logger.LogInformation("Contract payment notification created for user {UserId}, contract {ContractId}", contract.ParentId, contractId);
                }
                catch (Exception notificationEx)
                {
                    _logger.LogError(notificationEx, "Failed to create contract payment notification for contract {ContractId}", contractId);
                }
            }

            return new SePayWebhookResultDto
            {
                Success = true,
                Message = "Contract payment webhook processed successfully",
                OrderReference = sePayTransaction.OrderReference
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing contract payment webhook for contract {ContractId}", contractId);
            return new SePayWebhookResultDto
            {
                Success = false,
                Message = "An error occurred while processing contract payment webhook"
            };
        }
    }

    public string GenerateQrCodeUrl(decimal amount, string description)
    {
        var encodedDescription = HttpUtility.UrlEncode(description);
        return $"{_qrBaseUrl}?bank={_bankCode}&acc={_accountNumber}&template=compact&amount={amount:F0}&des={encodedDescription}";
    }

    public string? ExtractOrderReference(string content)
    {
        // Use regex to extract order reference (e.g., "MB12345678" from transaction content)
        var regex = new Regex($@"{_orderReferencePrefix}([A-Z0-9]+)", RegexOptions.IgnoreCase);
        var match = regex.Match(content);
        return match.Success ? match.Value : null;
    }

    public bool ValidateWebhookSignature(string payload, string? signature)
    {
        // SePay webhook signature validation
        // This would depend on SePay's signature implementation
        // For now, return true (implement proper validation if SePay provides signature mechanism)
        _logger.LogWarning("Webhook signature validation not implemented - accepting all webhooks");
        return true;
    }


    /// <inheritdoc />
    public async Task<SepayTransactionsByUserResponseDto> GetTransactionsByUserContractsAsync(Guid userId)
    {
        try
        {
            var transactions = await _sePayRepository.GetByParentIdThroughContractsAsync(userId);

            var transactionDtos = transactions.Select(t => new SepayTransactionItemDto
            {
                SepayTransactionId = t.SepayTransactionId,
                ContractId = t.ContractId,
                Gateway = t.Gateway,
                TransactionDate = t.TransactionDate,
                AccountNumber = t.AccountNumber,
                TransferType = t.TransferType,
                TransferAmount = t.TransferAmount,
                Code = t.Code,
                Content = t.Content,
                ReferenceNumber = t.ReferenceNumber,
                Description = t.Description,
                OrderReference = t.OrderReference,
                CreatedAt = t.CreatedAt,
                ChildName = t.Contract?.Child?.FullName,
                PackageName = t.Contract?.Package?.PackageName,
                ContractStatus = t.Contract?.Status
            }).ToList();

            return new SepayTransactionsByUserResponseDto
            {
                Success = true,
                Message = $"Found {transactionDtos.Count} transaction(s)",
                Transactions = transactionDtos
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting SePay transactions for user {UserId}", userId);
            return new SepayTransactionsByUserResponseDto
            {
                Success = false,
                Message = "An error occurred while retrieving transactions",
                Transactions = new List<SepayTransactionItemDto>()
            };
        }
    }
}