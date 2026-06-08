using System;
using System.Net;
using System.Net.Mail;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Repositories.Interfaces;
using Repositories.Models;

namespace Repositories.Implementations
{
    public class SmtpMailService : IMailService
    {
        private readonly SmtpSettings _smtpSettings;
        private readonly ILogger<SmtpMailService> _logger;

        public SmtpMailService(IOptions<SmtpSettings> smtpSettings, ILogger<SmtpMailService> logger)
        {
            _smtpSettings = smtpSettings.Value;
            _logger = logger;
        }

        public async Task SendEmailAsync(string toEmail, string subject, string htmlBody)
        {
            try
            {
                using (var message = new MailMessage())
                {
                    if (string.IsNullOrWhiteSpace(_smtpSettings.From))
                    {
                        throw new InvalidOperationException("SMTP 'From' address is not configured.");
                    }

                    message.From = new MailAddress(_smtpSettings.From, _smtpSettings.FromName);
                    message.To.Add(new MailAddress(toEmail));
                    message.Subject = subject;
                    message.Body = htmlBody;
                    message.IsBodyHtml = true;

                    using (var client = new SmtpClient(_smtpSettings.Host, _smtpSettings.Port))
                    {
                        client.Credentials = new NetworkCredential(_smtpSettings.UserName, _smtpSettings.Password);
                        client.EnableSsl = _smtpSettings.EnableSsl;
                        await client.SendMailAsync(message);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send email to {ToEmail} with subject {Subject}", toEmail, subject);
                throw; // Re-throw the exception to be handled by the caller
            }
        }

        public async Task SendOtpEmailAsync(string toEmail, string userName, string otpCode, int expiryMinutes)
        {
            var subject = "Your One-Time Password (OTP)";

            string templatePath = Path.Combine(AppContext.BaseDirectory, "Templates", "otp_email.html");
            string htmlBody;

            if (File.Exists(templatePath))
            {
                htmlBody = await File.ReadAllTextAsync(templatePath);
                htmlBody = htmlBody.Replace("{{USER_NAME}}", WebUtility.HtmlEncode(userName ?? string.Empty))
                                   .Replace("{{OTP_CODE}}", WebUtility.HtmlEncode(otpCode ?? string.Empty))
                                   .Replace("{{OTP_EXPIRY_MINUTES}}", expiryMinutes.ToString())
                                   .Replace("{{CURRENT_YEAR}}", DateTime.UtcNow.Year.ToString())
                                   .Replace("{{APP_NAME}}", WebUtility.HtmlEncode(_smtpSettings.FromName ?? "Outing"));
            }
            else
            {
                htmlBody = $@"<p>Hello {WebUtility.HtmlEncode(userName ?? string.Empty)},</p>
                <p>Your One-Time Password (OTP) is: <strong>{WebUtility.HtmlEncode(otpCode ?? string.Empty)}</strong></p>
                <p>This OTP is valid for {expiryMinutes} minutes.</p>
                <p>If you did not request this, please ignore this email.</p>
                <p>Thank you,</p>
                <p>{WebUtility.HtmlEncode(_smtpSettings.FromName ?? "Outing")}</p>";
            }

            await SendEmailAsync(toEmail, subject, htmlBody);
        }
    }
}