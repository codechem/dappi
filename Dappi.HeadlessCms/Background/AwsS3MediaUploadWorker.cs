using Dappi.HeadlessCms.Core.Requests;
using Dappi.HeadlessCms.Enums;
using Dappi.HeadlessCms.Interfaces;
using Dappi.HeadlessCms.Interfaces.StorageServices;
using Dappi.HeadlessCms.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Dappi.HeadlessCms.Background;

public class AwsS3MediaUploadWorker(
    IMediaUploadQueue queue,
    IServiceScopeFactory scopeFactory,
    ILogger<AwsS3MediaUploadWorker> logger)
    : BaseMediaUploadWorker<IAwsS3StorageService>(queue, scopeFactory, logger)
{
    protected override async Task ProcessRequestAsync(IAwsS3StorageService service, MediaUploadRequest request, CancellationToken ct)
    {
        await service.UpdateStatusAsync(request.MediaId, MediaUploadStatus.Pending);

        await service.UploadToS3Async(request.MediaId, request.StreamAndExtensionPair);
        
        await service.UpdateStatusAsync(request.MediaId, MediaUploadStatus.Completed);
    }
    
    protected override async Task HandleFailureAsync(IAwsS3StorageService service, MediaUploadRequest request, Exception ex)
    {
        await service.UpdateStatusAsync(request.MediaId, MediaUploadStatus.Failed);
    }
}