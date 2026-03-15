using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Polaris.Application.Common.Interfaces
{
    public interface ILinkGeneratorService
    {
        string GenerateEmailConfirmationLink(Guid userId, string token);
        string GeneratePasswordResetLink(string email, string token);
    }
}