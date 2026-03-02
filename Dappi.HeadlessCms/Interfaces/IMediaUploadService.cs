using Dappi.HeadlessCms.Core.Requests;
using Dappi.HeadlessCms.Enums;
using Dappi.HeadlessCms.Models;
using Microsoft.AspNetCore.Http;

namespace Dappi.HeadlessCms.Interfaces
{
    public interface IMediaUploadService
    {
        public void DeleteMedia(MediaInfo media);
        Task UpdateStatusAsync(Guid mediaId, MediaUploadStatus status);
        Task SaveFileAsync(Guid mediaId, StreamAndExtensionPair streamAndExtensionPair);
        Task SaveFileAsync(Guid mediaId, IFormFile file);
        public void ValidateFile(IFormFile file);
    }
}
