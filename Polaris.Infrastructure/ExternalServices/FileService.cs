using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using Microsoft.Extensions.Configuration;
using Polaris.Application.Common.Interfaces;

namespace Polaris.Infrastructure.ExternalServices
{
    public class FileService : IFileService
    {
        private readonly Cloudinary _cloudinary;

        public FileService(IConfiguration configuration)
        {
            var cloudinaryUrl = configuration["Cloudinary:Url"]
                ?? configuration["CLOUDINARY_URL"]
                ?? Environment.GetEnvironmentVariable("CLOUDINARY_URL")
                ?? throw new InvalidOperationException("Cloudinary URL must be set (Cloudinary:Url or CLOUDINARY_URL).");

            _cloudinary = new Cloudinary(cloudinaryUrl);
            _cloudinary.Api.Secure = true;
        }

        public async Task DeleteFileAsync(string filePath, string folderName, CancellationToken cancellationToken = default)
        {
            var publicId = $"{folderName}/{filePath}";
            var deleteParams = new DeletionParams(publicId);
            var deleteResult = await _cloudinary.DestroyAsync(deleteParams);

            if (deleteResult.Result != "ok")
                throw new InvalidOperationException($"Failed to delete file: {deleteResult.Error?.Message}");
        }

        public async Task<string> UploadFileAsync(Stream fileStream, string fileName, string folder, CancellationToken cancellationToken = default)
        {
            if (fileStream == null || fileStream.Length == 0)
                throw new ArgumentException("File stream is required.", nameof(fileStream));

            var uploadParams = new ImageUploadParams
            {
                File = new FileDescription(fileName, fileStream),
                Folder = folder
            };

            var uploadResult = await _cloudinary.UploadAsync(uploadParams);

            if (uploadResult.StatusCode != System.Net.HttpStatusCode.OK)
                throw new InvalidOperationException($"Failed to upload file: {uploadResult.Error?.Message}");

            var fileUrl = uploadResult.SecureUrl.ToString();

            if (fileName.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase)
                || fileName.EndsWith(".docx", StringComparison.OrdinalIgnoreCase)
                || fileName.EndsWith(".txt", StringComparison.OrdinalIgnoreCase))
            {
                return $"{fileUrl}?attachment=true";
            }

            return fileUrl;
        }
    }
}
