namespace CCApi.Extensions.DependencyInjection.Constants
{
    public static class UserRoles
    {
        public const string Admin = "Admin";
        public const string Maintainer = "Maintainer";
        public const string User = "User";

        public static readonly string[] All = { Admin, Maintainer, User };
    }
}