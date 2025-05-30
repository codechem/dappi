using System.Text.RegularExpressions;

namespace Dappi.HeadlessCms.Extensions
{
    public static class PropertyCheckUtils
    {
        public static bool PropertyNameExists(string classCode, string fieldName)
        {
            var pattern = $@"\b(?:public|private|protected|internal|static|readonly|required|virtual|abstract|sealed|unsafe|new|partial|\s)*\s*[\w<>\[\]\?]+\s+{Regex.Escape(fieldName)}\b\s*{{";            
            return Regex.IsMatch(classCode, pattern, RegexOptions.IgnoreCase);
        }
    }
}