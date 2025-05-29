using System.Text.RegularExpressions;

namespace CCApi.Extensions.DependencyInjection.Extensions
{
    public static class PropertyCheckUtils
    {
        public static bool PropertyNameExists(string classCode, string fieldName)
        {
            var pattern = $@"\bpublic\s+(?:[\w\.<>\[\]?]+)\s+\b{Regex.Escape(fieldName)}\b\s*{{";
            return Regex.IsMatch(classCode, pattern, RegexOptions.IgnoreCase);
        }
    }
}