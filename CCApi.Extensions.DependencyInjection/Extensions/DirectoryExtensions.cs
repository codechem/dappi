namespace CCApi.Extensions.DependencyInjection.Extensions
{
    public static class DirectoryUtils
    {
        public static List<string?> GetClassNamesFromDirectory(string directoryPath)
        {
            return Directory
                .GetFiles(directoryPath, "*.cs")
                .Select(Path.GetFileNameWithoutExtension)
                .ToList();
        }
    }
}