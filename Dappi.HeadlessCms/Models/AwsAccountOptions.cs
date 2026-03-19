namespace Dappi.HeadlessCms.Models;

public class AwsAccountOptions
{
    public const string AwsAccount = "AWS:Account";
    public string? AccessKey { get; set; }
    public string? SecretKey { get; set; }
    public string? Region { get; set; }
}
