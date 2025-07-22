using Dappi.HeadlessCms.Models;
using Microsoft.AspNetCore.Http;

namespace Dappi.HeadlessCms.Interfaces
{
    public interface IMediaUploadService
    {
        public Task<MediaInfo> UploadMediaAsync(Guid id, IFormFile file);
    }
}