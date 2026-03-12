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
    public class AwsS3StorageService(
        IConfiguration configuration,
        IDbContextAccessor dbContext,
        IS3ClientFactory factory
    ) : IMediaUploadService
    {
        private readonly IAmazonS3 _s3Client = factory.CreateClient();

        public void DeleteMedia(MediaInfo media)
        {
            if (string.IsNullOrEmpty(media.Url))
                return;

            var bucketName = configuration["AWS:Storage:BucketName"];

            var uri = new Uri(media.Url);
            var objectKey = Path.GetFileName(uri.LocalPath);

            var deleteRequest = new DeleteObjectRequest
            {
                BucketName = bucketName,
                Key = objectKey,
            };

            _s3Client.DeleteObjectAsync(deleteRequest).GetAwaiter().GetResult();
        }

        public async Task SaveFileAsync(Guid mediaId, IFormFile file)
        {
            if (file == null || file.Length == 0)
                throw new Exception("No file was uploaded.");

            var pair = await StreamAndExtensionPair.CreateFromFormFile(file);

            await SaveFileAsync(mediaId, pair);
        }

        public async Task SaveFileAsync(Guid mediaId, StreamAndExtensionPair streamAndExtensionPair)
        {
            var bucketName = configuration["AWS:Storage:BucketName"];
            var cdnUrl = configuration["AWS:Storage:CdnUrl"];
            var regionName = configuration["AWS:Account:Region"];

            var useCdn =
                bool.TryParse(configuration["AWS:Storage:UseCdn"], out var parsed) && parsed;

            var extension = streamAndExtensionPair.Extension.StartsWith(".")
                ? streamAndExtensionPair.Extension
                : "." + streamAndExtensionPair.Extension;

            var objectKey = $"{mediaId}{extension}";

            var region = Amazon.RegionEndpoint.GetBySystemName(regionName ?? "eu-central-1");

            var putRequest = new PutObjectRequest
            {
                BucketName = bucketName,
                Key = objectKey,
                InputStream = streamAndExtensionPair.Stream,
                AutoCloseStream = true,
                ContentType = GetContentType(extension),
            };
            await _s3Client.PutObjectAsync(putRequest);

            var baseUrl = useCdn
                ? $"{cdnUrl}/{objectKey}"
                : $"https://{bucketName}.s3.{region.SystemName}.amazonaws.com/{objectKey}";

            var media = await dbContext
                .DbContext.Set<MediaInfo>()
                .FirstOrDefaultAsync(m => m.Id == mediaId);

            if (media != null)
            {
                media.Url = baseUrl;
                await dbContext.DbContext.SaveChangesAsync();
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
            var media = await dbContext
                .DbContext.Set<MediaInfo>()
                .FirstOrDefaultAsync(m => m.Id == mediaId);

            if (media == null)
                return;

            media.Status = status;
            await dbContext.DbContext.SaveChangesAsync();
        }

        private string GetContentType(string extension) =>
            extension.ToLower() switch
            {
                ".jpg" or ".jpeg" => "image/jpeg",
                ".png" => "image/png",
                ".pdf" => "application/pdf",
                _ => "application/octet-stream",
            };

        public override string ToString() => "aws-s3";
    }
}
