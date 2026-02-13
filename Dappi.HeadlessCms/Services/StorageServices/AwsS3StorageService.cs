using Amazon.S3;
using Amazon.S3.Model;
using Dappi.HeadlessCms.Core.Requests;
using Dappi.HeadlessCms.Enums;
using Dappi.HeadlessCms.Interfaces;
using Dappi.HeadlessCms.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace Dappi.HeadlessCms.Services.StorageServices
{
    public class AwsS3StorageService(IConfiguration configuration, IDbContextAccessor dbContext) : IMediaUploadService
    {
        public void DeleteMedia(MediaInfo media)
        {
            // TODO: implement deletion of objects on s3
        }

        public async Task SaveFileAsync(Guid mediaId, StreamAndExtensionPair streamAndExtensionPair)
        {
            var accessKey = configuration["AWS:AccessKey"];
            var secretKey = configuration["AWS:SecretKey"];
            var regionName = configuration["AWS:Region"];
            var bucketName = configuration["AwsS3BucketConfiguration:BucketName"];

            if (string.IsNullOrEmpty(accessKey) || string.IsNullOrEmpty(secretKey))
            {
                throw new Exception("AWS Credentials are missing from configuration.");
            }

            var region = Amazon.RegionEndpoint.GetBySystemName(regionName ?? "eu-central-1");

            var extension = streamAndExtensionPair.Extension.StartsWith(".")
                ? streamAndExtensionPair.Extension
                : "." + streamAndExtensionPair.Extension;

            var objectKey = $"{mediaId:D}{extension}";

            var credentials = new Amazon.Runtime.BasicAWSCredentials(accessKey, secretKey);
            using var client = new AmazonS3Client(credentials, region);

            try
            {
                var putRequest = new PutObjectRequest
                {
                    BucketName = bucketName,
                    Key = objectKey,
                    InputStream = streamAndExtensionPair.Stream,
                    AutoCloseStream = true,
                    ContentType = GetContentType(streamAndExtensionPair.Extension)
                };

                await client.PutObjectAsync(putRequest);

                var s3Url = $"https://{bucketName}.s3.{region.SystemName}.amazonaws.com/{objectKey}";

                var media = await dbContext.DbContext.Set<MediaInfo>()
                    .FirstOrDefaultAsync(m => m.Id == mediaId);

                if (media != null)
                {
                    media.Url = s3Url;
                    await dbContext.DbContext.SaveChangesAsync();
                }
            }
            catch (AmazonS3Exception e)
            {
                throw new Exception($"Error encountered on server. Message:'{e.Message}' when writing an object", e);
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