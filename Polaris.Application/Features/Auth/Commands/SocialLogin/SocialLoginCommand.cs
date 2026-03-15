using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Polaris.Application.Features.Auth.Commands.SocialLogin
{
    public class SocialLoginCommand : IRequest<SocialLoginResult>
    {
        public string Email { get; set; } = string.Empty;
        public string? Name { get; set; }
        public string Provider { get; set; } = string.Empty;
        public string ProviderId { get; set; } = string.Empty;
    }
}
