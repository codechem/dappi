namespace Dappi.HeadlessCms.Models
{
    public class CreateSignedUrlRequestDto()
    {
        public required string ObjectKey { get; set; }
        public double? TimeToLiveInHours { get; set; }
    }
}