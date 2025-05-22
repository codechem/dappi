namespace Microsoft.Extensions.DependencyInjection;

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
}