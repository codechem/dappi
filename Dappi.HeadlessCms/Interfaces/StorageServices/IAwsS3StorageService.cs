using Dappi.HeadlessCms.Core.Requests;
using Dappi.HeadlessCms.Enums;
using Dappi.HeadlessCms.Models;
using Microsoft.AspNetCore.Http;

namespace Dappi.HeadlessCms.Interfaces.StorageServices
{
    public interface IAwsS3StorageService
    {
        Task UploadToS3Async(Guid mediaId, StreamAndExtensionPair streamAndExtensionPair);
        Task UpdateStatusAsync(Guid mediaId, MediaUploadStatus status);
        public void DeleteMedia(MediaInfo media);
        public void ValidateFile(IFormFile file);
    }
}