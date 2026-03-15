using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Polaris.Application.Features.Auth.Commands.ConfirmEmail
{
    public class ConfirmEmailCommand : IRequest<ConfirmEmailResult>
    {
        public Guid UserId { get; set; }
        public string Token { get; set; } = string.Empty;
    }
}
