using Dappi.HeadlessCms;
using Dappi.HeadlessCms.Models;
using MyCompany.MyProject.WebApi.Data;

namespace MyCompany.MyProject.WebApi;

public class Program
{
    private static async Task Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.Services.AddDappi<AppDbContext>(builder.Configuration);

        var awsAccount = builder.Configuration.GetSection("AWS:Account").Get<AwsAccountOptions>();
        if (awsAccount is not null
            && !string.IsNullOrWhiteSpace(awsAccount.AccessKey)
            && !string.IsNullOrWhiteSpace(awsAccount.SecretKey)
            && !string.IsNullOrWhiteSpace(awsAccount.Region)
            && !string.IsNullOrWhiteSpace(awsAccount.BucketName))
        {
            builder.Services.AddS3Storage(builder.Configuration);
        }

        var awsSes = builder.Configuration.GetSection("AWS:SES").Get<AwsSesOptions>();
        if (awsSes is not null
            && !string.IsNullOrWhiteSpace(awsSes.AccessKey)
            && !string.IsNullOrWhiteSpace(awsSes.SecretKey)
            && !string.IsNullOrWhiteSpace(awsSes.SourceEmail))
        {
            builder.Services.AddAwsSes(builder.Configuration);
        }

        builder.Services.AddDappiAuthentication<DappiUser, DappiRole, AppDbContext>(builder.Configuration);

        var app = builder.Build();

        await app.UseDappi<AppDbContext>();
        
        app.UseHttpsRedirection();
        app.MapControllers();
        
        app.Run();
    }
}