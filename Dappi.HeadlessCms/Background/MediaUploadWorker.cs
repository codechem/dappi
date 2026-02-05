using Dappi.HeadlessCms.Enums;
using Dappi.HeadlessCms.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Dappi.HeadlessCms.Background;

public class MediaUploadWorker(
    IMediaUploadQueue queue,
    IServiceScopeFactory scopeFactory,
    ILogger<MediaUploadWorker> logger)
    : BackgroundService
{
    protected override async Task ExecuteAsync(
        CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            using var scope = scopeFactory.CreateScope();
            var uploadService = scope.ServiceProvider.GetRequiredService<IMediaUploadService>();
            await foreach (var request in queue.GetReader().ReadAllAsync(stoppingToken))
            {
                var status = MediaUploadStatus.Pending;
                await uploadService.UpdateStatusAsync(
                    request.MediaId,
                    status);
                try
                {
                    logger.LogInformation("File upload started for mediaId {RequestMediaId}", request.MediaId);

                    await uploadService.SaveFileAsync(
                        request.MediaId, request.StreamAndExtensionPair);

                    status = MediaUploadStatus.Completed;
                }
                catch(Exception ex)
                {
                    logger.LogError("File upload failed for mediaId {RequestMediaId}: {ExMessage}", request.MediaId, ex.Message); 
                    status = MediaUploadStatus.Failed;
                }
                finally
                {
                    await uploadService.UpdateStatusAsync(
                        request.MediaId,
                        status);
                    logger.LogInformation("File upload completed for mediaId {RequestMediaId}", request.MediaId);
                }
            }
        }
    }
}