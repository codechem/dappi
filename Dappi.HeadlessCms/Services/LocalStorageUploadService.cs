using System.Net.Mime;
using Dappi.HeadlessCms.Interfaces;
using Dappi.HeadlessCms.Models;
using Microsoft.AspNetCore.Http;

namespace Dappi.HeadlessCms.Services
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

            var relativePath = $"uploads{Path.DirectorySeparatorChar}{fileName}";

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

        public void DeleteMedia(MediaInfo media)
        {
            var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", media.Url);
            if (File.Exists(filePath))
            {
                try
                {
                    File.Delete(filePath);
                }
                catch (Exception ex)
                {
                    // Handle exception
                }
            }
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

        public async Task<MediaInfo> SaveFileAsync(Guid id, IFormFile file)
        {
            var uploadsFolder = Path.Combine(
                Directory.GetCurrentDirectory(),
                "wwwroot",
                "uploads");

            if (!Directory.Exists(uploadsFolder))
                Directory.CreateDirectory(uploadsFolder);

            var fileExtension = Path.GetExtension(file.FileName);
            var fileName = $"{Guid.NewGuid()}_{id}{fileExtension}";
            var filePath = Path.Combine(uploadsFolder, fileName);

            await using (var fileStream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(fileStream);
            }

            var relativePath = $"uploads{Path.DirectorySeparatorChar}{fileName}";

            return new MediaInfo
            {
                Id = Guid.NewGuid(),
                Url = relativePath,
                OriginalFileName = file.FileName,
                FileSize = file.Length,
                UploadDate = DateTime.UtcNow
            };
        }
    }
}