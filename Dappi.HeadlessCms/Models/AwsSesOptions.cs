namespace Dappi.HeadlessCms.Models;

public class AwsSesOptions
{
    public const string AwsSes = "AWS:SES";
    public string? AccessKey { get; set; }
    public string? SecretKey { get; set; }
    public string? SourceEmail { get; set; }
}
