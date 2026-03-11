using Amazon.SimpleEmail;
using Microsoft.Extensions.Configuration;

namespace Dappi.HeadlessCms.Interfaces;

public interface ISesClientFactory
{
    IAmazonSimpleEmailService CreateClient();
}

public class SesClientFactory(IConfiguration configuration) : ISesClientFactory
{
    public IAmazonSimpleEmailService CreateClient()
    {
        var accessKey = configuration["AWS:SES:AccessKey"];
        var secretKey = configuration["AWS:SES:SecretKey"];
        return new AmazonSimpleEmailServiceClient(accessKey, secretKey);
    }
}
