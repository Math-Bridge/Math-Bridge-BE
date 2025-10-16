using MathBridgeSystem.Domain.Entities;

namespace MathBridgeSystem.Application.Interfaces
{
    /// <summary>
    /// Repository interface for SePay transaction data access
    /// </summary>
    public interface ISePayRepository
    {
        /// <summary>
        /// Add a new SePay transaction record
        /// </summary>
        /// <param name="transaction">SePay transaction entity</param>
        /// <returns>Created transaction</returns>
        Task<SepayTransaction> AddAsync(SepayTransaction transaction);

        /// <summary>
        /// Get SePay transaction by ID
        /// </summary>
        /// <param name="id">Transaction ID</param>
        /// <returns>SePay transaction or null</returns>
        Task<SepayTransaction?> GetByIdAsync(Guid id);

        /// <summary>
        /// Get SePay transaction by wallet transaction ID
        /// </summary>
        /// <param name="walletTransactionId">Wallet transaction ID</param>
        /// <returns>SePay transaction or null</returns>
        Task<SepayTransaction?> GetByWalletTransactionIdAsync(Guid walletTransactionId);

        /// <summary>
        /// Get SePay transaction by order reference
        /// </summary>
        /// <param name="orderReference">Order reference from transaction content</param>
        /// <returns>SePay transaction or null</returns>
        Task<SepayTransaction?> GetByOrderReferenceAsync(string orderReference);

        /// <summary>
        /// Check if SePay transaction already exists by SePay code
        /// </summary>
        /// <param name="code">SePay transaction code</param>
        /// <returns>True if transaction exists</returns>
        Task<SepayTransaction?> ExistsByCodeAsync(string code);

        /// <summary>
        /// Get SePay transactions by user ID through wallet transactions
        /// </summary>
        /// <param name="userId">User ID</param>
        /// <param name="pageNumber">Page number for pagination</param>
        /// <param name="pageSize">Page size for pagination</param>
        /// <returns>List of SePay transactions</returns>
        Task<IEnumerable<SepayTransaction>> GetByUserIdAsync(Guid userId, int pageNumber = 1, int pageSize = 10);

        /// <summary>
        /// Update SePay transaction
        /// </summary>
        /// <param name="transaction">SePay transaction to update</param>
        /// <returns>Updated transaction</returns>
        Task<SepayTransaction> UpdateAsync(SepayTransaction transaction);

        /// <summary>
        /// Get pending SePay transactions (wallet transactions with Pending status)
        /// </summary>
        /// <returns>List of pending transactions</returns>
        Task<IEnumerable<SepayTransaction>> GetPendingTransactionsAsync();

        /// <summary>
        /// Get SePay transactions within date range
        /// </summary>
        /// <param name="startDate">Start date</param>
        /// <param name="endDate">End date</param>
        /// <returns>List of transactions</returns>
        Task<IEnumerable<SepayTransaction>> GetByDateRangeAsync(DateTime startDate, DateTime endDate);
    }
}
