using MathBridge.Domain.Entities;

namespace MathBridge.Application.Interfaces
{
    /// <summary>
    /// Repository interface for PayOS transaction data access
    /// </summary>
    public interface IPayOSRepository
    {
        /// <summary>
        /// Create a new PayOS transaction record
        /// </summary>
        /// <param name="transaction">PayOS transaction entity</param>
        /// <returns>Created transaction</returns>
        Task<PayOSTransaction> CreateAsync(PayOSTransaction transaction);

        /// <summary>
        /// Get PayOS transaction by order code
        /// </summary>
        /// <param name="orderCode">PayOS order code</param>
        /// <returns>PayOS transaction or null</returns>
        Task<PayOSTransaction?> GetByOrderCodeAsync(long orderCode);

        /// <summary>
        /// Get PayOS transaction by ID
        /// </summary>
        /// <param name="id">PayOS transaction ID</param>
        /// <returns>PayOS transaction or null</returns>
        Task<PayOSTransaction?> GetByIdAsync(Guid id);

        /// <summary>
        /// Get PayOS transaction by wallet transaction ID
        /// </summary>
        /// <param name="walletTransactionId">Wallet transaction ID</param>
        /// <returns>PayOS transaction or null</returns>
        Task<PayOSTransaction?> GetByWalletTransactionIdAsync(Guid walletTransactionId);

        /// <summary>
        /// Update PayOS transaction
        /// </summary>
        /// <param name="transaction">PayOS transaction to update</param>
        /// <returns>Updated transaction</returns>
        Task<PayOSTransaction> UpdateAsync(PayOSTransaction transaction);

        /// <summary>
        /// Get PayOS transactions by user ID through wallet transactions
        /// </summary>
        /// <param name="userId">User ID</param>
        /// <param name="skip">Number of records to skip</param>
        /// <param name="take">Number of records to take</param>
        /// <returns>List of PayOS transactions</returns>
        Task<IEnumerable<PayOSTransaction>> GetByUserIdAsync(Guid userId, int skip = 0, int take = 10);

        /// <summary>
        /// Get pending PayOS transactions older than specified minutes
        /// </summary>
        /// <param name="olderThanMinutes">Minutes threshold for old pending transactions</param>
        /// <returns>List of pending transactions</returns>
        Task<IEnumerable<PayOSTransaction>> GetPendingTransactionsAsync(int olderThanMinutes = 30);
    }
}