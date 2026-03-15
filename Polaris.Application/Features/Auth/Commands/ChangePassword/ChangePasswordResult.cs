using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Polaris.Application.Features.Auth.Commands.ChangePassword
{
    public class ChangePasswordResult
    {
        public bool Succeeded { get; set; }
        public string? Message { get; set; }
    }
}
