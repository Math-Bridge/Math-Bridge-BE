using System.Threading.Tasks;

namespace MathBridgeSystem.Application.Interfaces
{
    public interface IEmailService
    {
        // Authentication Emails
        Task SendVerificationLinkAsync(string email, string link);
        Task SendResetPasswordLinkAsync(string email, string link);

        // Notification Emails
        Task SendSessionReminderAsync(string email, string studentName, string tutorName, string sessionDateTime, string sessionLink);
        Task SendInvoiceAsync(string email, string studentName, string invoiceNumber, string amount, string dueDate, string invoiceUrl);
        Task SendProgressReportAsync(string email, string studentName, string reportPeriod, string reportUrl);
        Task SendRefundConfirmationAsync(string email, string studentName, string refundAmount, string refundDate);
        Task SendContractCancelledAsync(string email, string studentName, string reason, string cancellationDate);
        Task SendContractConfirmationAsync(
    string email,
    string parentName,
    Guid contractId,
    byte[] pdfBytes,
    string pdfFileName = "MathBridge_Contract.pdf");

        // Report Emails
        Task SendReportSubmittedAsync(string email, string parentName, Guid reportId);
        Task SendReportStatusUpdateAsync(string email, string parentName, Guid reportId, string status, string reason);
    }
}