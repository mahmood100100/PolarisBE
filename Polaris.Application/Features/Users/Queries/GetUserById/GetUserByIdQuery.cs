using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Polaris.Application.Features.Users.Queries.GetUserById
{
    public class GetUserByIdQuery : IRequest<UserDetailsDto>
    {
        public Guid Id { get; set; }
    }
}
