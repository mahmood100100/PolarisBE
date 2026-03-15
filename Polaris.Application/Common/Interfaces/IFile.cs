using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Polaris.Application.Common.Interfaces
{
    public interface IFile
    {
        string FileName { get; }
        long Length { get; }
        Stream OpenReadStream();
    }
}
