using Microsoft.Extensions.Configuration;

namespace Microsoft.Extensions.DependencyInjection;

public static class ConfigurationExtensions
{
    public static bool IsDappiUiConfigured(this IConfiguration configuration)
    {
        return configuration.GetValue<string?>(Constants.Configuration.FrontendUrl) is not null;
    }
}