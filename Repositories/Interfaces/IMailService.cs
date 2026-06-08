using System.Threading.Tasks;

namespace Repositories.Interfaces
{
    public interface IMailService
    {
        // Generic send method: pass HTML body and subject
        Task SendEmailAsync(string toEmail, string subject, string htmlBody);

        // Convenience helper for OTP emails
        Task SendOtpEmailAsync(string toEmail, string userName, string otpCode, int expiryMinutes);
    }
}
