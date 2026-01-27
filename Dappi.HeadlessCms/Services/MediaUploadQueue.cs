using System.Threading.Channels;
using Dappi.HeadlessCms.Core.Requests;
using Dappi.HeadlessCms.Interfaces;

namespace Dappi.HeadlessCms.Services
{
    public class MediaUploadQueue : IMediaUploadQueue
    {
        private readonly Channel<MediaUploadRequest> _channel = Channel.CreateBounded<MediaUploadRequest>(
            new BoundedChannelOptions(100)
            {
                SingleReader = true,
                SingleWriter = false,
                FullMode = BoundedChannelFullMode.Wait
            });

        public ValueTask EnqueueAsync(MediaUploadRequest request)
            => _channel.Writer.WriteAsync(request);

        public ChannelReader<MediaUploadRequest> GetReader() => _channel.Reader;
    }
}