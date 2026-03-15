using Polaris.Application.Common.Interfaces;

namespace Polaris.WebAPI.Common.Adapters
{
    public class FormFileAdapter : IFile
    {
        private readonly IFormFile _formFile;

        public FormFileAdapter(IFormFile formFile)
        {
            _formFile = formFile;
        }

        public string FileName => _formFile.FileName;
        public long Length => _formFile.Length;
        public Stream OpenReadStream() => _formFile.OpenReadStream();
    }
}
