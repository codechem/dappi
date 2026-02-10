using Dappi.HeadlessCms.Core.Requests;
using Dappi.HeadlessCms.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Dappi.HeadlessCms.Background;

public abstract class BaseMediaUploadWorker<TService>(
    IMediaUploadQueue queue,
    IServiceScopeFactory scopeFactory,
    ILogger logger) : BackgroundService where TService : notnull
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await foreach (var request in queue.GetReader().ReadAllAsync(stoppingToken))
            {
                using var scope = scopeFactory.CreateScope();
                var service = scope.ServiceProvider.GetRequiredService<TService>();

                logger.LogInformation("Processing started for mediaId {MediaId}", request.MediaId);

                try
                {
                    await ProcessRequestAsync(service, request, stoppingToken);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Processing failed for mediaId {MediaId}", request.MediaId);
                    await HandleFailureAsync(service, request, ex);
                }
                finally
                {
                    logger.LogInformation("Processing finished for mediaId {MediaId}", request.MediaId);
                }
            }
        }
    }

    protected abstract Task ProcessRequestAsync(TService service, MediaUploadRequest request, CancellationToken ct);
    protected virtual Task HandleFailureAsync(TService service, MediaUploadRequest request, Exception ex) 
        => Task.CompletedTask;
}