using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Polaris.Application.Features.Auth.Commands.ResendConfirmationEmail
{
    public class ResendConfirmationCommand : IRequest<ResendConfirmationResult>
    {
        public string Email { get; set; } = string.Empty;
    }
}
