using System.Reflection;

namespace Dappi.SourceGenerator.Utilities;

public static class EmbeddedResourceLoader
{
    public static string LoadEmbeddedTemplate(string embeddedResourceName)
    {
        var assembly = Assembly.GetExecutingAssembly();
        using var stream = assembly.GetManifestResourceStream(embeddedResourceName);
        if (stream is null)
        {
            throw new InvalidOperationException(
                $"Embedded resource '{embeddedResourceName}' not found."
            );
        }

        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }
}
