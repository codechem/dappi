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
        private readonly AwsStorageOptions _storageOptions = configuration
            .GetSection(AwsStorageOptions.AwsStorage)
            .Get<AwsStorageOptions>()!;
        private readonly AwsAccountOptions _accountOptions = configuration
            .GetSection(AwsAccountOptions.AwsAccount)
            .Get<AwsAccountOptions>()!;

        public void DeleteMedia(MediaInfo media)
        {
            if (string.IsNullOrEmpty(media.Url))
                return;

            var uri = new Uri(media.Url);
            var objectKey = Path.GetFileName(uri.LocalPath);

            var deleteRequest = new DeleteObjectRequest
            {
                BucketName = _storageOptions.BucketName,
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
            var extension = streamAndExtensionPair.Extension.StartsWith(".")
                ? streamAndExtensionPair.Extension
                : "." + streamAndExtensionPair.Extension;

            var objectKey = $"{mediaId}{extension}";

            var region = Amazon.RegionEndpoint.GetBySystemName(
                _accountOptions.Region ?? "eu-central-1"
            );

            var putRequest = new PutObjectRequest
            {
                BucketName = _storageOptions.BucketName,
                Key = objectKey,
                InputStream = streamAndExtensionPair.Stream,
                AutoCloseStream = true,
                ContentType = GetContentType(extension),
            };
            await _s3Client.PutObjectAsync(putRequest);

            var baseUrl = !string.IsNullOrEmpty(_storageOptions.CdnUrl)
                ? $"{_storageOptions.CdnUrl}/{objectKey}"
                : $"https://{_storageOptions.BucketName}.s3.{region.SystemName}.amazonaws.com/{objectKey}";

            var media = await dbContext
                .DbContext.Set<MediaInfo>()
                .FirstOrDefaultAsync(m => m.Id == mediaId);

            if (media != null)
            {
                media.Url = baseUrl;
                await dbContext.DbContext.SaveChangesAsync();
            }
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
