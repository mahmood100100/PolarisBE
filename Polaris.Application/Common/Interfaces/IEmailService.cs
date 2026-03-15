using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Polaris.Application.Common.Interfaces
{
    public interface IEmailService
    {
        Task SendEmailAsync(string to, string subject, string body);
        Task SendEmailWithTemplateAsync(string to, string subject, string templateName, Dictionary<string, string> placeholders);
        Task SendWelcomeEmailAsync(string to, string name);
        Task SendPasswordResetEmailAsync(string to, string numericToken);
        Task SendEmailConfirmationAsync(string to, string confirmationLink, string name);
    }
}
