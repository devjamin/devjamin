/*
    EmailService.cs, sends emails to myself using the provided email settings.
*/
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;

namespace devjamin.Services
{
    public interface IEmailService
    {
        Task SendEmailAsync(string name, string from, string subject, string body);
    }

    public class EmailService : IEmailService
    {
        private readonly EmailSettings _emailSettings;

        public EmailService(IOptions<EmailSettings> emailSettings)
        {
            _emailSettings = emailSettings.Value ?? throw new ArgumentNullException(nameof(emailSettings), "Email settings must be provided.");
            if (string.IsNullOrWhiteSpace(_emailSettings.SmtpServer))
            {
                throw new ArgumentException("SMTP server configuration is missing.", nameof(emailSettings));
            }
        }

        public async Task SendEmailAsync(string name, string from, string subject, string body)
        {
            if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(from) ||
                string.IsNullOrWhiteSpace(subject) || string.IsNullOrWhiteSpace(body))
            {
                throw new ArgumentException("Name, from, subject, and body fields must not be empty.");
            }

            var emailMessage = new MimeMessage();
            emailMessage.From.Add(new MailboxAddress(name, from));
            emailMessage.To.Add(MailboxAddress.Parse(_emailSettings.To)); // Always to email address from settings
            emailMessage.Subject = subject;

            // Replace newlines with HTML line breaks
            var formattedBody = body.Replace("\n", "<br>");
            var bodyBuilder = new BodyBuilder
            {
                HtmlBody = formattedBody
            };
            emailMessage.Body = bodyBuilder.ToMessageBody();

            using var client = new SmtpClient();
            try
            {
                // SecureSocketOptions.Auto automatically selects the best security option
                await client.ConnectAsync(_emailSettings.SmtpServer, _emailSettings.SmtpPort, _emailSettings.EnableSSL ? SecureSocketOptions.Auto : SecureSocketOptions.None);

                if (!string.IsNullOrWhiteSpace(_emailSettings.SmtpUsername) && !string.IsNullOrWhiteSpace(_emailSettings.SmtpPassword))
                {
                    // Only authenticate if credentials are provided
                    await client.AuthenticateAsync(_emailSettings.SmtpUsername, _emailSettings.SmtpPassword);
                }

                await client.SendAsync(emailMessage);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to send email: {ex.Message}", ex);
            }
            finally
            {
                await client.DisconnectAsync(true);
            }
        }

        public class EmailSettings
        {
            public string? To { get; set; }
            public string? SmtpServer { get; set; }
            public int SmtpPort { get; set; }
            public string? SmtpUsername { get; set; }
            public string? SmtpPassword { get; set; }
            public bool EnableSSL { get; set; } = true;
        }
    }
}
