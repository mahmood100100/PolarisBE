using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Polaris.Application.Features.Auth.Commands.Logout
{
    public class LogoutCommand : IRequest<bool>
    {
        public Guid UserId { get; set; }
        public string? RefreshToken { get; set; }
        public bool LogoutAllDevices { get; set; }
    }
}
