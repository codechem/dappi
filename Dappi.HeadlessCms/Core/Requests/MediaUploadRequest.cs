using Dappi.HeadlessCms.Models;
using Microsoft.AspNetCore.Http;

namespace Dappi.HeadlessCms.Core.Requests
{
    // this is a channel payload
    // in our UploadMediaAsync we have the id of the entity, the file, and we return mediaInfo
    // our channel can only do one job and that is why we wrap these in a request
    public record MediaUploadRequest(
        Guid MediaId,
        StreamAndExtensionPair StreamAndExtensionPair
    );

    public record StreamAndExtensionPair(Stream Stream, string Extension)
    {
        public static async Task<StreamAndExtensionPair> CreateFromFormFile(IFormFile file)
        {
            var memoryStream = new MemoryStream();
            await file.CopyToAsync(memoryStream);
            memoryStream.Position = 0;
            return new StreamAndExtensionPair(memoryStream, Path.GetExtension(file.FileName));    
        }
    }
}