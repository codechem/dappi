using Amazon;
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
        var accessKey = configuration["AWS:Account:AccessKey"];
        var secretKey = configuration["AWS:Account:SecretKey"];
        var regionName = configuration["AWS:Account:Region"];
        var region = RegionEndpoint.GetBySystemName(regionName);
        return new AmazonSimpleEmailServiceClient(accessKey, secretKey, region);
    }
}
