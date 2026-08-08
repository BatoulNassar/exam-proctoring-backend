using ExamProctoring.Application.Common;
using ExamProctoring.Application.Common.Interfaces;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Mail;

namespace ExamProctoring.Infrastructure.Services
{
    public class EmailService : IEmailService
    {
        private readonly EmailSettings _settings;

        public EmailService(IOptions<EmailSettings> settings)
        {
            _settings = settings.Value;
        }

        /// <summary>
        /// Fails with the names of the missing keys rather than letting SmtpClient
        /// throw on an empty host. Configuration is absent far more often than SMTP
        /// itself is broken, and the two look identical in the logs otherwise.
        /// </summary>
        private void EnsureConfigured()
        {
            var missing = new List<string>();

            if (string.IsNullOrWhiteSpace(_settings.SmtpHost)) missing.Add("Email:SmtpHost");
            if (_settings.SmtpPort <= 0) missing.Add("Email:SmtpPort");
            if (string.IsNullOrWhiteSpace(_settings.From)) missing.Add("Email:From");
            if (string.IsNullOrWhiteSpace(_settings.Username)) missing.Add("Email:Username");
            if (string.IsNullOrWhiteSpace(_settings.Password)) missing.Add("Email:Password");

            if (missing.Count == 0) return;

            throw new EmailNotConfiguredException(
                $"Email is not configured: {string.Join(", ", missing)}. " +
                "On a server these come from environment variables such as Email__SmtpHost.");
        }

        public async Task SendAsync(string to, string subject, string body)
        {
            EnsureConfigured();

            using var client = new SmtpClient(_settings.SmtpHost, _settings.SmtpPort)
            {
                EnableSsl = true,
                Credentials = new NetworkCredential(_settings.Username, _settings.Password)
            };

            var message = new MailMessage
            {
                From = new MailAddress(_settings.From, "Exam Proctoring"),
                Subject = subject,
                Body = body,
                IsBodyHtml = true
            };
            message.To.Add(to);

            await client.SendMailAsync(message);
        }
    }
}
