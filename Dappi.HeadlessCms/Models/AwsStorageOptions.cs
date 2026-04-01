namespace Dappi.HeadlessCms.Models;

public class AwsStorageOptions
{
    public const string AwsStorage = "AWS:Storage";
    public string? BucketName { get; set; }
    public string? CdnUrl { get; set; }
    public List<string>? SupportedExtensions { get; set; }
}
