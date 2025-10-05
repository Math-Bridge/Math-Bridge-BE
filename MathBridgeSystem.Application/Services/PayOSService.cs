using MathBridge.Application.DTOs.PayOS;
using MathBridge.Application.Interfaces;
using MathBridge.Domain.Entities;
using MathBridge.Domain.Interfaces;
using MathBridge.Infrastructure.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Net.payOS.Types;

namespace MathBridge.Application.Services;

/// <summary>
/// Service implementation for PayOS payment gateway operations
/// Handles payment link creation, webhook processing, and status management
/// </summary>
public class PayOSService : IPayOSService
{
    private readonly IPayOSRepository _payOSRepository;
    private readonly IWalletTransactionRepository _walletTransactionRepository;
    private readonly IUserRepository _userRepository;
    private readonly PayOSGatewayService _payOSGatewayService;
    private readonly IConfiguration _configuration;
    private readonly ILogger<PayOSService> _logger;

    // PayOS configuration
    private readonly string _returnUrl;
    private readonly string _cancelUrl;

    public PayOSService(
        IPayOSRepository payOSRepository,
        IWalletTransactionRepository walletTransactionRepository,
        IUserRepository userRepository,
        PayOSGatewayService payOSGatewayService,
        IConfiguration configuration,
        ILogger<PayOSService> logger)
    {
        _payOSRepository = payOSRepository;
        _walletTransactionRepository = walletTransactionRepository;
        _userRepository = userRepository;
        _payOSGatewayService = payOSGatewayService;
        _configuration = configuration;
        _logger = logger;

        // Load configuration
        _returnUrl = _configuration["PayOS:ReturnUrl"] ?? "https://yourdomain.com/payment/success";
        _cancelUrl = _configuration["PayOS:CancelUrl"] ?? "https://yourdomain.com/payment/cancel";
    }

    public async Task<PayOSPaymentResponse> CreatePaymentLinkAsync(CreatePayOSPaymentRequest request)
    {
        try
        {
            _logger.LogInformation("Creating PayOS payment link for user {UserId}, amount {Amount}",
                request.UserId, request.Amount);

            // Validate request
            if (request.Amount <= 0)
            {
                return new PayOSPaymentResponse
                {
                    Success = false,
                    Message = "Amount must be greater than 0"
                };
            }

            // Check if user exists
            var user = await _userRepository.GetByIdAsync(request.UserId);
            if (user == null)
            {
                return new PayOSPaymentResponse
                {
                    Success = false,
                    Message = "User not found"
                };
            }

            // Generate unique order code
            var orderCode = GenerateOrderCode();

            // Create wallet transaction
            var walletTransaction = new WalletTransaction
            {
                TransactionId = Guid.NewGuid(),
                ParentId = request.UserId,
                Amount = request.Amount,
                TransactionType = "Deposit",
                Description = $"PayOS deposit: {request.Description}",
                TransactionDate = DateTime.UtcNow,
                Status = "Pending",
                PaymentMethod = "PayOS",
                PaymentGateway = "PayOS",
                PaymentGatewayReference = orderCode.ToString()
            };

            var createdWalletTransaction = await _walletTransactionRepository.AddAsync(walletTransaction);

            // Create PayOS payment data
            var paymentData = new PaymentData(
                orderCode: orderCode,
                amount: (int)request.Amount,
                description: request.Description ?? $"Deposit {request.Amount:N0} VND",
                items: new List<ItemData>
                {
                    new ItemData(
                        name: "Wallet Deposit",
                        quantity: 1,
                        price: (int)request.Amount
                    )
                },
                cancelUrl: request.CancelUrl ?? _cancelUrl,
                returnUrl: request.ReturnUrl ?? _returnUrl
            );

            // Call PayOS API to create payment link
            var paymentResult = await _payOSGatewayService.CreatePaymentLinkAsync(paymentData);

            // Create PayOS transaction record
            var payOSTransaction = new PayOSTransaction
            {
                PayosTransactionId = Guid.NewGuid(),
                WalletTransactionId = createdWalletTransaction.TransactionId,
                OrderCode = orderCode,
                PaymentLinkId = paymentResult.paymentLinkId,
                CheckoutUrl = paymentResult.checkoutUrl,
                PaymentStatus = "PENDING",
                Amount = request.Amount,
                Description = request.Description,
                ReturnUrl = request.ReturnUrl ?? _returnUrl,
                CancelUrl = request.CancelUrl ?? _cancelUrl,
                CreatedDate = DateTime.UtcNow,
                UpdatedDate = DateTime.UtcNow
            };

            var createdPayOSTransaction = await _payOSRepository.CreateAsync(payOSTransaction);

            _logger.LogInformation("Successfully created PayOS payment link. Order code: {OrderCode}, Checkout URL: {CheckoutUrl}",
                orderCode, paymentResult.checkoutUrl);

            return new PayOSPaymentResponse
            {
                Success = true,
                Message = "Payment link created successfully",
                CheckoutUrl = paymentResult.checkoutUrl,
                OrderCode = orderCode,
                PaymentLinkId = paymentResult.paymentLinkId,
                WalletTransactionId = createdWalletTransaction.TransactionId,
                PayosTransactionId = createdPayOSTransaction.PayosTransactionId,
                Amount = request.Amount,
                Status = "PENDING"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating PayOS payment link for user {UserId}", request.UserId);
            return new PayOSPaymentResponse
            {
                Success = false,
                Message = $"Error creating payment link: {ex.Message}"
            };
        }
    }

    public async Task<PayOSWebhookResult> ProcessWebhookAsync(PayOSWebhookRequest webhookData)
    {
        try
        {
            _logger.LogInformation("Processing PayOS webhook for order code: {OrderCode}",
                webhookData.Data?.OrderCode ?? 0);

            // Verify webhook signature
            if (!VerifyWebhookSignature(webhookData))
            {
                _logger.LogWarning("Invalid webhook signature for order code: {OrderCode}",
                    webhookData.Data?.OrderCode ?? 0);
                return new PayOSWebhookResult
                {
                    Success = false,
                    Message = "Invalid webhook signature"
                };
            }

            if (webhookData.Data == null)
            {
                return new PayOSWebhookResult
                {
                    Success = false,
                    Message = "Webhook data is null"
                };
            }

            // Find PayOS transaction
            var payOSTransaction = await _payOSRepository.GetByOrderCodeAsync(webhookData.Data.OrderCode);
            if (payOSTransaction == null)
            {
                _logger.LogWarning("PayOS transaction not found for order code: {OrderCode}",
                    webhookData.Data.OrderCode);
                return new PayOSWebhookResult
                {
                    Success = false,
                    Message = "Transaction not found"
                };
            }

            // Update PayOS transaction status
            if (webhookData.Code == "00" && webhookData.Success)
            {
                payOSTransaction.PaymentStatus = "PAID";
                payOSTransaction.PaidAt = DateTime.UtcNow;
                payOSTransaction.UpdatedDate = DateTime.UtcNow;

                // Update wallet transaction
                var walletTransaction = payOSTransaction.WalletTransaction;
                walletTransaction.Status = "Completed";
                
                await _walletTransactionRepository.UpdateAsync(walletTransaction);
                await _payOSRepository.UpdateAsync(payOSTransaction);

                _logger.LogInformation("Payment completed for order code: {OrderCode}", webhookData.Data.OrderCode);

                return new PayOSWebhookResult
                {
                    Success = true,
                    Message = "Payment processed successfully",
                    WalletTransactionId = payOSTransaction.WalletTransactionId,
                    OrderCode = webhookData.Data.OrderCode,
                    PaymentStatus = "PAID"
                };
            }
            else
            {
                payOSTransaction.PaymentStatus = "CANCELLED";
                payOSTransaction.UpdatedDate = DateTime.UtcNow;

                // Update wallet transaction
                var walletTransaction = payOSTransaction.WalletTransaction;
                walletTransaction.Status = "Failed";
                
                await _walletTransactionRepository.UpdateAsync(walletTransaction);
                await _payOSRepository.UpdateAsync(payOSTransaction);

                _logger.LogInformation("Payment cancelled for order code: {OrderCode}", webhookData.Data.OrderCode);

                return new PayOSWebhookResult
                {
                    Success = true,
                    Message = "Payment cancellation processed",
                    WalletTransactionId = payOSTransaction.WalletTransactionId,
                    OrderCode = webhookData.Data.OrderCode,
                    PaymentStatus = "CANCELLED"
                };
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing PayOS webhook");
            return new PayOSWebhookResult
            {
                Success = false,
                Message = $"Error processing webhook: {ex.Message}"
            };
        }
    }

    public async Task<PayOSPaymentStatusResponse> CheckPaymentStatusAsync(long orderCode)
    {
        try
        {
            _logger.LogInformation("Checking payment status for order code: {OrderCode}", orderCode);

            var payOSTransaction = await _payOSRepository.GetByOrderCodeAsync(orderCode);
            if (payOSTransaction == null)
            {
                return new PayOSPaymentStatusResponse
                {
                    Success = false,
                    Message = "Transaction not found",
                    OrderCode = orderCode
                };
            }

            return new PayOSPaymentStatusResponse
            {
                Success = true,
                Message = "Status retrieved successfully",
                Status = payOSTransaction.PaymentStatus,
                OrderCode = orderCode,
                Amount = payOSTransaction.Amount,
                PaidAt = payOSTransaction.PaidAt,
                PaymentLinkId = payOSTransaction.PaymentLinkId,
                WalletTransactionId = payOSTransaction.WalletTransactionId
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking payment status for order code: {OrderCode}", orderCode);
            return new PayOSPaymentStatusResponse
            {
                Success = false,
                Message = $"Error checking status: {ex.Message}",
                OrderCode = orderCode
            };
        }
    }

    public async Task<PayOSPaymentResponse?> GetPaymentDetailsByWalletTransactionAsync(Guid walletTransactionId)
    {
        try
        {
            var payOSTransaction = await _payOSRepository.GetByWalletTransactionIdAsync(walletTransactionId);
            if (payOSTransaction == null)
            {
                return null;
            }

            return new PayOSPaymentResponse
            {
                Success = true,
                Message = "Payment details retrieved",
                CheckoutUrl = payOSTransaction.CheckoutUrl ?? "",
                OrderCode = payOSTransaction.OrderCode,
                PaymentLinkId = payOSTransaction.PaymentLinkId,
                WalletTransactionId = payOSTransaction.WalletTransactionId,
                PayosTransactionId = payOSTransaction.PayosTransactionId,
                Amount = payOSTransaction.Amount,
                Status = payOSTransaction.PaymentStatus
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting payment details for wallet transaction: {WalletTransactionId}",
                walletTransactionId);
            return null;
        }
    }

    public async Task<CancelPayOSPaymentResponse> CancelPaymentAsync(CancelPayOSPaymentRequest request)
    {
        try
        {
            _logger.LogInformation("Cancelling payment for order code: {OrderCode}", request.OrderCode);

            var payOSTransaction = await _payOSRepository.GetByOrderCodeAsync(request.OrderCode);
            if (payOSTransaction == null)
            {
                return new CancelPayOSPaymentResponse
                {
                    Success = false,
                    Message = "Transaction not found",
                    OrderCode = request.OrderCode
                };
            }

            if (payOSTransaction.PaymentStatus != "PENDING")
            {
                return new CancelPayOSPaymentResponse
                {
                    Success = false,
                    Message = $"Cannot cancel payment with status: {payOSTransaction.PaymentStatus}",
                    OrderCode = request.OrderCode,
                    Status = payOSTransaction.PaymentStatus
                };
            }

            // Cancel in PayOS
            var cancelResult = await _payOSGatewayService.CancelPaymentLinkAsync(
                request.OrderCode,
                request.CancellationReason);

            // Update local status
            payOSTransaction.PaymentStatus = "CANCELLED";
            payOSTransaction.UpdatedDate = DateTime.UtcNow;
            await _payOSRepository.UpdateAsync(payOSTransaction);

            // Update wallet transaction
            var walletTransaction = payOSTransaction.WalletTransaction;
            walletTransaction.Status = "Cancelled";
            await _walletTransactionRepository.UpdateAsync(walletTransaction);

            _logger.LogInformation("Successfully cancelled payment for order code: {OrderCode}", request.OrderCode);

            return new CancelPayOSPaymentResponse
            {
                Success = true,
                Message = "Payment cancelled successfully",
                OrderCode = request.OrderCode,
                Status = "CANCELLED"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cancelling payment for order code: {OrderCode}", request.OrderCode);
            return new CancelPayOSPaymentResponse
            {
                Success = false,
                Message = $"Error cancelling payment: {ex.Message}",
                OrderCode = request.OrderCode
            };
        }
    }

    public async Task<PayOSTransactionsResponse> GetUserTransactionsAsync(GetPayOSTransactionsRequest request)
    {
        try
        {
            var skip = (request.PageNumber - 1) * request.PageSize;
            var transactions = await _payOSRepository.GetByUserIdAsync(
                request.UserId,
                skip,
                request.PageSize);

            var transactionDtos = transactions.Select(t => new PayOSTransactionDto
            {
                PayosTransactionId = t.PayosTransactionId,
                WalletTransactionId = t.WalletTransactionId,
                OrderCode = t.OrderCode,
                PaymentLinkId = t.PaymentLinkId,
                CheckoutUrl = t.CheckoutUrl,
                PaymentStatus = t.PaymentStatus,
                Amount = t.Amount,
                Description = t.Description,
                CreatedDate = t.CreatedDate,
                UpdatedDate = t.UpdatedDate,
                PaidAt = t.PaidAt
            }).ToList();

            return new PayOSTransactionsResponse
            {
                Success = true,
                Message = "Transactions retrieved successfully",
                Transactions = transactionDtos,
                TotalCount = transactionDtos.Count,
                PageNumber = request.PageNumber,
                PageSize = request.PageSize
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting transactions for user: {UserId}", request.UserId);
            return new PayOSTransactionsResponse
            {
                Success = false,
                Message = $"Error retrieving transactions: {ex.Message}",
                Transactions = new List<PayOSTransactionDto>()
            };
        }
    }

    public long GenerateOrderCode()
    {
        // Generate unique order code using timestamp
        // PayOS requires a unique long integer
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        var random = new Random().Next(1000, 9999);
        
        // Combine timestamp with random number to ensure uniqueness
        // Keep it within long range
        var orderCode = (timestamp % 100000000) * 10000 + random;
        
        return orderCode;
    }

    public bool VerifyWebhookSignature(PayOSWebhookRequest webhookData)
    {
        try
        {
            // Convert webhook request to PayOS SDK WebhookType
            var webhookType = new WebhookType(
                code: webhookData.Code,
                desc: webhookData.Description,
                success: webhookData.Success,
                data: webhookData.Data != null ? new WebhookData(
                    orderCode: webhookData.Data.OrderCode,
                    amount: (int)webhookData.Data.Amount,
                    description: webhookData.Data.Description,
                    accountNumber: webhookData.Data.AccountNumber,
                    reference: webhookData.Data.Reference,
                    transactionDateTime: webhookData.Data.TransactionDateTime,
                    currency: webhookData.Data.Currency,
                    paymentLinkId: webhookData.Data.PaymentLinkId,
                    code: webhookData.Data.Code,
                    desc: webhookData.Data.Desc,
                    counterAccountBankId: webhookData.Data.CounterAccountBankId,
                    counterAccountBankName: webhookData.Data.CounterAccountBankName,
                    counterAccountName: webhookData.Data.CounterAccountName,
                    counterAccountNumber: webhookData.Data.CounterAccountNumber,
                    virtualAccountName: webhookData.Data.VirtualAccountName,
                    virtualAccountNumber: webhookData.Data.VirtualAccountNumber
                ) : null,
                signature: webhookData.Signature
            );

            var verifiedData = _payOSGatewayService.VerifyWebhookData(webhookType);
            return verifiedData != null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error verifying webhook signature");
            return false;
        }
    }

    public async Task<PayOSPaymentStatusResponse> SyncPaymentStatusAsync(long orderCode)
    {
        try
        {
            _logger.LogInformation("Syncing payment status with PayOS for order code: {OrderCode}", orderCode);

            // Get status from PayOS API
            var paymentInfo = await _payOSGatewayService.GetPaymentInfoAsync(orderCode);

            // Update local database
            var payOSTransaction = await _payOSRepository.GetByOrderCodeAsync(orderCode);
            if (payOSTransaction == null)
            {
                return new PayOSPaymentStatusResponse
                {
                    Success = false,
                    Message = "Transaction not found locally",
                    OrderCode = orderCode
                };
            }

            // Map PayOS status to our status
            var newStatus = paymentInfo.status switch
            {
                "PAID" => "PAID",
                "CANCELLED" => "CANCELLED",
                "PENDING" => "PENDING",
                _ => "PENDING"
            };

            if (payOSTransaction.PaymentStatus != newStatus)
            {
                payOSTransaction.PaymentStatus = newStatus;
                payOSTransaction.UpdatedDate = DateTime.UtcNow;

                if (newStatus == "PAID")
                {
                    payOSTransaction.PaidAt = DateTime.UtcNow;
                    
                    // Update wallet transaction
                    var walletTransaction = payOSTransaction.WalletTransaction;
                    walletTransaction.Status = "Completed";
                    await _walletTransactionRepository.UpdateAsync(walletTransaction);
                }

                await _payOSRepository.UpdateAsync(payOSTransaction);
            }

            return new PayOSPaymentStatusResponse
            {
                Success = true,
                Message = "Status synced successfully",
                Status = newStatus,
                OrderCode = orderCode,
                Amount = payOSTransaction.Amount,
                PaidAt = payOSTransaction.PaidAt,
                PaymentLinkId = payOSTransaction.PaymentLinkId,
                WalletTransactionId = payOSTransaction.WalletTransactionId
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error syncing payment status for order code: {OrderCode}", orderCode);
            return new PayOSPaymentStatusResponse
            {
                Success = false,
                Message = $"Error syncing status: {ex.Message}",
                OrderCode = orderCode
            };
        }
    }
}