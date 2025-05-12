using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CCApi.Extensions.DependencyInjection.Models;
using Microsoft.AspNetCore.Http;

namespace CCApi.Extensions.DependencyInjection.Interfaces
{
    public interface IMediaUploadService
    {
        public Task<MediaInfo> UploadMediaAsync(Guid id, IFormFile file);
    }
}