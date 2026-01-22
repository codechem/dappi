using Dappi.HeadlessCms.Enums;
using Dappi.HeadlessCms.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Dappi.HeadlessCms.Background
{
    public class MediaUploadWorker(
        IMediaUploadQueue queue,
        IServiceScopeFactory scopeFactory)
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
                    try
                    {
                        await uploadService.SaveFileAsync(
                            request.MediaId,
                            request.File);

                        status = MediaUploadStatus.Completed;
                    }
                    catch
                    {
                        status = MediaUploadStatus.Failed;
                    }
                    finally
                    {
                        await uploadService.UpdateStatusAsync(
                            request.MediaId,
                            status);
                    }
                }
            }
        }
    }
}