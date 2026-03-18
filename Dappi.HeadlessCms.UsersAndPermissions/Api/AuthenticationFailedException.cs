namespace Dappi.HeadlessCms.UsersAndPermissions.Api
{
    public class AuthenticationFailedException(
        string? message = "Authentication failed. Please check your credentials and try again."
    ) : Exception(message);
}
