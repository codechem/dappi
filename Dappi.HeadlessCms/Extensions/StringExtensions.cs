using Microsoft.CodeAnalysis.CSharp;

namespace Dappi.HeadlessCms.Extensions
{
    public static class StringExtensions
    {
        // Still missing to check if name is an interface, if it's possible.
        public static bool IsValidClassNameOrPropertyName(this string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                return false;
            }

            if (name.Any(char.IsWhiteSpace)
                || name.Any(char.IsSeparator)
                || name.Any(char.IsPunctuation)
                || char.IsDigit(name[0]))
            {
                return false;
            }
            var contextualKeywordKind = SyntaxFacts.GetContextualKeywordKind(name);
            if (SyntaxFacts.IsContextualKeyword(contextualKeywordKind))
            {
                return false;
            }

            // if it is a reserved keyword it will be parsed successfully to a Keyword kind.
            var keywordKind = SyntaxFacts.GetKeywordKind(name);
            
            return !SyntaxFacts.IsKeywordKind(keywordKind);
        }
        
        public static object ConvertValue(this string value)
        {
            ArgumentNullException.ThrowIfNull(value);
            var values = value.Split(',');
            var convertedValues = new List<object>();
            
            foreach (var v in values)
            {
                if (int.TryParse(v, out var intValue))
                {
                    convertedValues.Add(intValue);
                }
                else if (DateOnly.TryParse(v, out var dateOnlyValue))
                {
                    convertedValues.Add(v);
                }
                else if (DateTime.TryParse(v, out var dateTimeValue))
                {
                    convertedValues.Add(v);
                }
                else if (bool.TryParse(v, out var boolValue))
                {
                    convertedValues.Add(boolValue);
                }
                else if (Guid.TryParse(v, out var guidValue))
                {
                    convertedValues.Add(guidValue);
                }
                else
                {
                    convertedValues.Add(v);
                }
            }
            
            return convertedValues.Count > 1 ? convertedValues : convertedValues.First();
        }
    }
}