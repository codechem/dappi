using Amazon;
using Amazon.SimpleEmail;
using Dappi.HeadlessCms.Models;
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
        var accountOptions =
            configuration.GetSection(AwsAccountOptions.AwsAccount).Get<AwsAccountOptions>()
            ?? new AwsAccountOptions();

        return new AmazonSimpleEmailServiceClient(
            accountOptions.AccessKey,
            accountOptions.SecretKey,
            RegionEndpoint.GetBySystemName(accountOptions.Region)
        );
    }
}
