using MediatR;
using Polaris.Application.Features.Auth.Commands.Login;
using Polaris.Application.Features.Users.Queries.GetUserById;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Polaris.Application.Features.Users.Queries.GetCurrentUser
{
    public class GetCurrentUserQuery : IRequest<UserDetailsDto?>
    {
    }
}
