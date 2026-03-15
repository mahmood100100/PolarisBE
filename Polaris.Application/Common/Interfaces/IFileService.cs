namespace Polaris.Application.Common.Interfaces
{
    public interface IFileService
    {
        Task<string> UploadFileAsync(Stream fileStream, string fileName, string folder, CancellationToken cancellationToken = default);

        Task DeleteFileAsync(string filePath, string folderName, CancellationToken cancellationToken = default);
    }
}
