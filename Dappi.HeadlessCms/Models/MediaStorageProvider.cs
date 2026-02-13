namespace Dappi.HeadlessCms.Models
{
    public enum MediaStorageProvider
    {
        Local = 0,
        AwsS3BucketStorage = 1,
        AzureBlobStorage = 2,
        GoogleCloudStorage = 3
    }
}