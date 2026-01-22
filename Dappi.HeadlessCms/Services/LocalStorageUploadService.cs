using System.Net.Mime;
using Dappi.HeadlessCms.Database;
using Dappi.HeadlessCms.Enums;
using Dappi.HeadlessCms.Interfaces;
using Dappi.HeadlessCms.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace Dappi.HeadlessCms.Services
{
    public class LocalStorageUploadService(IDbContextAccessor _dbContext) : IMediaUploadService
    {
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
            
            var media = await _dbContext.DbContext.Set<MediaInfo>()
                .Where(m => m.Id == mediaId).FirstOrDefaultAsync();

            if (media == null) return ;

            media.Url = relativePath;
            await _dbContext.DbContext.SaveChangesAsync();
        }

        public async Task UpdateStatusAsync(Guid mediaId, MediaUploadStatus status)
        {
            var media = await _dbContext.DbContext.Set<MediaInfo>()
                .Where(m => m.Id == mediaId).FirstOrDefaultAsync();

            if (media == null) return;

            media.Status = status;
            await _dbContext.DbContext.SaveChangesAsync();
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