namespace Dappi.HeadlessCms.UsersAndPermissions.Core
{
    public static class UsersAndPermissionsConstants
    {
        public const string SystemId = "Dappi.UsersAndPermissions";

        /// <summary>
        /// The policy scheme that acts as the single <c>DefaultAuthenticateScheme</c>.
        /// It peeks at the incoming JWT's <c>iss</c> claim and forwards to the correct
        /// named bearer scheme (Dappi-internal or any registered external provider).
        /// </summary>
        public const string SelectorSchemeId = "Dappi.SchemeSelector";

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
            public const string Refresh = "refresh";
        }
    }
}
