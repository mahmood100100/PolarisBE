using MediatR;
using Polaris.Application.Common.Interfaces;
using Polaris.Domain.Common.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Polaris.Application.Features.Users.Commands.RegisterUser
{
    public class RegisterUserCommand : IRequest<RegisterUserResult>
    {
        public string FullName { get; set; }
        public string UserName { get; set; }
        public string Email { get; set; }
        public string Password { get; set; }
        public IFile? ProfileImage { get; set; }
    }
}
