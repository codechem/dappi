using System.Net.Mime;
using CCApi.Extensions.DependencyInjection.Interfaces;
using CCApi.Extensions.DependencyInjection.Models;
using Microsoft.AspNetCore.Http;

namespace CCApi.Extensions.DependencyInjection.Services
{
    public class LocalStorageUploadService : IMediaUploadService
    {
        public async Task<MediaInfo> UploadMediaAsync(Guid id, IFormFile file)
        {
            if (file == null || file.Length == 0)
                throw new Exception("No file was uploaded.");

            var fileExtension = Path.GetExtension(file.FileName);

            if (GetContentType(fileExtension) == "unsupported")
                throw new Exception("Unsupported media type.");

            var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads");

            if (!Directory.Exists(uploadsFolder))
            {
                Directory.CreateDirectory(uploadsFolder);
            }


            var fileName = $"{Guid.NewGuid()}_{id}{fileExtension}";
            var filePath = Path.Combine(uploadsFolder, fileName);

            using (var fileStream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(fileStream);
            }

            var relativePath = $"/uploads/{fileName}";

            var mediaInfo = new MediaInfo
            {
                Id = Guid.NewGuid(),
                Url = relativePath,
                OriginalFileName = file.FileName,
                FileSize = file.Length,
                UploadDate = DateTime.UtcNow
            };


            return mediaInfo;
        }

        private string GetContentType(string fileExtension)
        {
            return fileExtension.ToLower() switch
            {
                ".pdf" => MediaTypeNames.Application.Pdf,
                ".jpg" or ".jpeg" => MediaTypeNames.Image.Jpeg,
                ".png" => MediaTypeNames.Image.Png,
                ".gif" => MediaTypeNames.Image.Gif,
                _ => "unsupported",
            };
        }
    }
}