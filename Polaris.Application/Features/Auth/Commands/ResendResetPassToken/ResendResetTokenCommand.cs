using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Polaris.Application.Features.Auth.Commands.ResendResetPassToken
{
    public class ResendResetTokenCommand : IRequest<ResendResetTokenResult>
    {
        public string Email { get; set; }
    }

}
