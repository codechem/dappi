namespace Dappi.HeadlessCms.Extensions;

public static class ServiceProviderExtensions
{
    public static Dictionary<Type, object> ResolveAvailable(
        this IServiceProvider serviceProvider,
        params Type[] interfaceTypes
    )
    {
        var result = new Dictionary<Type, object>();

        foreach (var type in interfaceTypes)
        {
            var implementation = serviceProvider.GetService(type);
            if (implementation != null)
            {
                result[type] = implementation;
            }
        }

        return result;
    }
}