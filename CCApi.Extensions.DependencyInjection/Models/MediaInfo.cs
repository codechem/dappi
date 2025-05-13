using System;
using System.ComponentModel.DataAnnotations;

namespace CCApi.Extensions.DependencyInjection.Models
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