using System.ComponentModel.DataAnnotations;

namespace Dappi.HeadlessCms.Models
{
    public class MediaInfo
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Url { get; set; }
        public string OriginalFileName { get; set; }
        public long FileSize { get; set; }
        public DateTime UploadDate { get; set; }
    }
}