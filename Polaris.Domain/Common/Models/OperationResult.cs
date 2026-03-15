using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Polaris.Domain.Common.Models
{
    public class OperationResult
    {
        public bool Succeeded { get; private set; }
        public string[] Errors { get; private set; }

        public static OperationResult Success() => new OperationResult { Succeeded = true, Errors = Array.Empty<string>() };
        public static OperationResult Failure(params string[] errors) => new OperationResult { Succeeded = false, Errors = errors };
    }
}
