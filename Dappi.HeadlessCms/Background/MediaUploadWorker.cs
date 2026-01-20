using Dappi.HeadlessCms.Interfaces;
using Dappi.HeadlessCms.Services;
using Microsoft.Extensions.Hosting;

namespace Dappi.HeadlessCms.Background
{
    public class MediaUploadWorker(
        IMediaUploadQueue queue,
        IMediaUploadService mediaService)
        : BackgroundService
    {
        protected override async Task ExecuteAsync(
            CancellationToken stoppingToken)
        {
            await foreach (var request in queue.GetReader().ReadAllAsync(stoppingToken))
            {
                try
                {
                    var result = await mediaService.SaveFileAsync(
                        request.Id, request.File);

                    request.Completion.SetResult(result);
                }
                catch (Exception ex)
                {
                    request.Completion.SetException(ex);
                }
            }
        }
    }
}