namespace Dappi.HeadlessCms;

internal static class Constants
{
    public static class CorsPolicies
    {
        public const string AllowDappiAngularApp = nameof(AllowDappiAngularApp);
    }

    public static class Configuration
    {
        private const string SectionName = "Dappi";
        public const string PostgresConnection = $"{SectionName}:{nameof(PostgresConnection)}";
        public const string FrontendUrl = $"{SectionName}:{nameof(FrontendUrl)}";
    }
    
    public static class UserRoles
    {
        public const string Admin = "Admin";
        public const string Maintainer = "Maintainer";
        public const string User = "User";

        public static readonly string[] All = { Admin, Maintainer, User };
    }

}