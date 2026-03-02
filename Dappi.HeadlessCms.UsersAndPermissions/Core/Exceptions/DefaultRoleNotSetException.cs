namespace Dappi.HeadlessCms.UsersAndPermissions.Core.Exceptions
{
    public class DefaultRoleNotSetException(
        string message = "Default role is not set. Please set a default role in the configuration."
    ) : Exception(message);
}
