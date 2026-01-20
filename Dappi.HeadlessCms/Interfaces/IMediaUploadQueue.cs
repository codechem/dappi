using System.Threading.Channels;
using Dappi.HeadlessCms.Core.Requests;

namespace Dappi.HeadlessCms.Interfaces
{
    public interface IMediaUploadQueue
    {
        public ValueTask EnqueueAsync(MediaUploadRequest request);
        public ChannelReader<MediaUploadRequest> GetReader();
    }
}