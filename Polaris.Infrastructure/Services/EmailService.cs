using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using MimeKit;
using MimeKit.Text;
using Polaris.Application.Common.Interfaces;
using Polaris.Infrastructure.Helpers;

namespace Polaris.Infrastructure.Services
{
    public class EmailService : IEmailService
    {
        private readonly IHostEnvironment _hostEnvironment;

        public EmailService(IHostEnvironment hostEnvironment)
        {
            _hostEnvironment = hostEnvironment;
        }

        public async Task SendEmailAsync(string to, string subject, string body)
        {
            var senderEmail = EnvironmentHelper.GetEnvironmentVariable("EmailSettings__SenderEmail");
            var password = EnvironmentHelper.GetEnvironmentVariable("EmailSettings__Password");
            var smtpServer = EnvironmentHelper.GetEnvironmentVariable("EmailSettings__SmtpServer", "smtp.gmail.com");
            var port = EnvironmentHelper.GetEnvironmentVariableInt("EmailSettings__Port", 587);


            var email = new MimeMessage();
            email.From.Add(new MailboxAddress("Polaris App", senderEmail));
            email.To.Add(new MailboxAddress("", to));
            email.Subject = subject;
            email.Body = new TextPart(TextFormat.Html) { Text = body };

            using var smtp = new SmtpClient();

            try
            {
                await smtp.ConnectAsync(smtpServer, port, SecureSocketOptions.StartTls);
                await smtp.AuthenticateAsync(senderEmail, password);
                await smtp.SendAsync(email);
                await smtp.DisconnectAsync(true);
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to send email: {ex.Message}", ex);
            }
        }

        public async Task SendEmailWithTemplateAsync(string to, string subject, string templateName, Dictionary<string, string> placeholders)
        {
            // Load template from file only
            var template = await LoadTemplateAsync(templateName);

            foreach (var placeholder in placeholders)
            {
                template = template.Replace($"{{{{{placeholder.Key}}}}}", placeholder.Value);
            }

            await SendEmailAsync(to, subject, template);
        }

        public async Task SendWelcomeEmailAsync(string to, string name)
        {
            var placeholders = new Dictionary<string, string>
            {
                { "name", name },
                { "email", to },
                { "year", DateTime.UtcNow.Year.ToString() }
            };

            await SendEmailWithTemplateAsync(to, "Welcome to Polaris! 🚀", "welcome", placeholders);
        }

        public async Task SendPasswordResetEmailAsync(string to, string numericToken)
        {

            var placeholders = new Dictionary<string, string>
    {
        { "token", numericToken },
        { "email", to },
        { "year", DateTime.UtcNow.Year.ToString() }
    };

            await SendEmailWithTemplateAsync(to, "Reset Your Password - Polaris", "password-reset", placeholders);
        }


        public async Task SendEmailConfirmationAsync(string to, string confirmationLink, string name)
        {
            var placeholders = new Dictionary<string, string> 
            {
                { "name", name },
                { "confirmationLink", confirmationLink },
                { "email", to },
                { "year", DateTime.UtcNow.Year.ToString() }
            };

            await SendEmailWithTemplateAsync(to, "Confirm Your Email - Polaris", "email-confirmation", placeholders);
        }

        private async Task<string> LoadTemplateAsync(string templateName)
        {
            // Try multiple paths to find the template
            string[] possiblePaths = new[]
            {
                // Path in WebAPI EmailTemplates folder
                Path.Combine(Directory.GetCurrentDirectory(), "EmailTemplates", $"{templateName}.html"),
                
                // Path in Infrastructure project (development)
                Path.Combine(Directory.GetCurrentDirectory(), "..", "Polaris.Infrastructure", "EmailTemplates", $"{templateName}.html"),
                
                // Path in bin folder
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "EmailTemplates", $"{templateName}.html")
            };

            foreach (var path in possiblePaths)
            {
                if (File.Exists(path))
                {
                    return await File.ReadAllTextAsync(path);
                }
            }

            throw new FileNotFoundException($"Email template '{templateName}.html' not found. Tried paths: {string.Join(", ", possiblePaths)}");
        }
    }
}