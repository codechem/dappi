namespace Dappi.HeadlessCms.UsersAndPermissions.Core
{
    public static class UsersAndPermissionsConstants
    {
        public const string SystemId = "Dappi.UsersAndPermissions";

        internal const string ConfigurationKey = "Dappi:UsersAndPermissionsSystem";

        public static class DefaultRoles
        {
            public const string Authenticated = nameof(Authenticated);
            public const string Public = nameof(Public);
        }

        public static class AuthenticationRoutes
        {
            public const string Login = "Login";
            public const string Register = "Register";
        }
    }
}
