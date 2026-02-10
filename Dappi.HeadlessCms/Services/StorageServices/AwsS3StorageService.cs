using Amazon.S3;
using Amazon.S3.Model;
using Dappi.HeadlessCms.Core.Requests;
using Dappi.HeadlessCms.Enums;
using Dappi.HeadlessCms.Interfaces;
using Dappi.HeadlessCms.Interfaces.StorageServices;
using Dappi.HeadlessCms.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace Dappi.HeadlessCms.Services.StorageServices
{
    public class AwsS3StorageService(IConfiguration configuration, IDbContextAccessor dbContext) : IAwsS3StorageService
    {
        public void DeleteMedia(MediaInfo media)
        {
            // TODO: implement deletion of objects on s3
        }
        
        public void ValidateFile(IFormFile file)
        {
            if (file == null || file.Length == 0)
                throw new Exception("No file was uploaded.");

            var fileExtension = Path.GetExtension(file.FileName);

            if (GetContentType(fileExtension) == "unsupported")
                throw new Exception("Unsupported media type.");
        }
        
        public async Task UploadToS3Async(Guid mediaId, StreamAndExtensionPair streamAndExtensionPair)
        {
            var bucketName = configuration["AwsS3BucketConfiguration:BucketName"];
            var objectKey = $"{mediaId}{streamAndExtensionPair.Extension}";
    
            using var client = new AmazonS3Client();

            var putRequest = new PutObjectRequest
            {
                BucketName = bucketName,
                Key = objectKey,
                InputStream = streamAndExtensionPair.Stream,
                AutoCloseStream = true,
                ContentType = GetContentType(streamAndExtensionPair.Extension) 
            };

            await client.PutObjectAsync(putRequest);

            var s3Url = $"https://{bucketName}.s3.amazonaws.com/{objectKey}";

            var media = await dbContext.DbContext.Set<MediaInfo>()
                .FirstOrDefaultAsync(m => m.Id == mediaId);

            if (media != null)
            {
                media.Url = s3Url;
                await dbContext.DbContext.SaveChangesAsync();
            }
        }

        public async Task UpdateStatusAsync(Guid mediaId, MediaUploadStatus status)
        {
            var media = await dbContext.DbContext.Set<MediaInfo>()
                .Where(m => m.Id == mediaId).FirstOrDefaultAsync();

            if (media == null) return;

            media.Status = status;
            await dbContext.DbContext.SaveChangesAsync();
        }
        
        private string GetContentType(string extension) => extension.ToLower() switch
        {
            ".jpg" or ".jpeg" => "image/jpeg",
            ".png" => "image/png",
            ".pdf" => "application/pdf",
            _ => "application/octet-stream"
        };
    }
}