using Amazon.S3;
using Amazon.SimpleEmail;
using Microsoft.Extensions.Configuration;

namespace Dappi.HeadlessCms.Interfaces;

public interface IS3ClientFactory
{
    IAmazonS3 CreateClient();
}

public class S3ClientFactory(IConfiguration configuration) : IS3ClientFactory
{
    public IAmazonS3 CreateClient()
    {
        var accessKey = configuration["AWS:SES:AccessKey"];
        var secretKey = configuration["AWS:SES:SecretKey"];
        var regionName = configuration["AWS:Account:Region"];
        return new AmazonS3Client(accessKey, secretKey, regionName);
    }
}
