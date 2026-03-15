using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Polaris.Application.Features.Auth.Commands.ConfirmEmail
{
    public class ConfirmEmailResult
    {
        public bool Succeeded { get; set; }
        public string? Message { get; set; }
    }
}
