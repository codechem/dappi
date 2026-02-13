using System.Net.Mime;
using Dappi.HeadlessCms.Core.Requests;
using Dappi.HeadlessCms.Enums;
using Dappi.HeadlessCms.Interfaces;
using Dappi.HeadlessCms.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace Dappi.HeadlessCms.Services.StorageServices
{
    public class LocalStorageUploadService(IDbContextAccessor dbContext) : IMediaUploadService
    {
        public void DeleteMedia(MediaInfo media)
        {
            if (media.Url == null) throw new ArgumentNullException(media.Url);
            var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", media.Url);
            if (File.Exists(filePath)) File.Delete(filePath);
        }

        public void ValidateFile(IFormFile file)
        {
            if (file == null || file.Length == 0)
                throw new Exception("No file was uploaded.");

            var fileExtension = Path.GetExtension(file.FileName);

            if (GetContentType(fileExtension) == "unsupported")
                throw new Exception("Unsupported media type.");
        }

        public async Task SaveFileAsync(Guid mediaId, IFormFile file)
        {
            var uploadsFolder = Path.Combine(
                Directory.GetCurrentDirectory(),
                "wwwroot",
                "uploads");

            if (!Directory.Exists(uploadsFolder))
                Directory.CreateDirectory(uploadsFolder);

            var fileExtension = Path.GetExtension(file.FileName);
            var fileName = $"{Guid.NewGuid()}_{mediaId}{fileExtension}";
            var filePath = Path.Combine(uploadsFolder, fileName);

            await using (var fileStream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(fileStream);
            }

            var relativePath = $"uploads{Path.DirectorySeparatorChar}{fileName}";

            var media = await dbContext.DbContext.Set<MediaInfo>()
                .Where(m => m.Id == mediaId).FirstOrDefaultAsync();

            if (media == null) return;

            media.Url = relativePath;
            await dbContext.DbContext.SaveChangesAsync();
        }
        
        public async Task SaveFileAsync(Guid mediaId, StreamAndExtensionPair streamAndExtensionPair)
        {
            var uploadsFolder = Path.Combine(
                Directory.GetCurrentDirectory(),
                "wwwroot",
                "uploads");

            if (!Directory.Exists(uploadsFolder))
                Directory.CreateDirectory(uploadsFolder);

            var fileName = $"{Guid.NewGuid()}_{mediaId}{streamAndExtensionPair.Extension}";
            var filePath = Path.Combine(uploadsFolder, fileName);

            await using (var fileStream = new FileStream(filePath, FileMode.Create))
            {
                await streamAndExtensionPair.Stream.CopyToAsync(fileStream); 
            }

            var relativePath = $"uploads{Path.DirectorySeparatorChar}{fileName}";

            var media = await dbContext.DbContext.Set<MediaInfo>()
                .Where(m => m.Id == mediaId).FirstOrDefaultAsync();

            if (media == null) return;

            media.Url = relativePath;
            await dbContext.DbContext.SaveChangesAsync();
        }

        public async Task UpdateStatusAsync(Guid mediaId, MediaUploadStatus status)
        {
            var media = await dbContext.DbContext.Set<MediaInfo>()
                .Where(m => m.Id == mediaId).FirstOrDefaultAsync();

            if (media == null) return;

            media.Status = status;
            await dbContext.DbContext.SaveChangesAsync();
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