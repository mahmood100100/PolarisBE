using Polaris.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace Polaris.Application.Common.Interfaces
{
    public interface ITokenService
    {
        string GenerateAccessToken(LocalUser user, IList<string> roles);
        string GenerateRefreshToken();
        ClaimsPrincipal? GetPrincipalFromExpiredToken(string token);
        ClaimsPrincipal? ValidateAccessToken(string token);
    }
}
