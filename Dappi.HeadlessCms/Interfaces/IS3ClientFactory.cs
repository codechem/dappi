using Amazon;
using Amazon.S3;
using Amazon.SimpleEmail;
using Dappi.HeadlessCms.Models;
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
        var accountOptions =
            configuration.GetSection(AwsAccountOptions.AwsAccount).Get<AwsAccountOptions>()
            ?? new AwsAccountOptions();

        return new AmazonS3Client(
            accountOptions.AccessKey,
            accountOptions.SecretKey,
            RegionEndpoint.GetBySystemName(accountOptions.Region)
        );
    }
}
