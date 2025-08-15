using Microsoft.CodeAnalysis.CSharp;
using Pluralize.NET;

namespace Dappi.HeadlessCms.Extensions
{
    public static class StringExtensions
    {
        static readonly IPluralize Pluralizer = new Pluralizer();
        
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

            // if it is a reserved keyword it will be parsed successfully to a Keyword kind.
            var keywordKind = SyntaxFacts.GetKeywordKind(name);
            
            return !SyntaxFacts.IsKeywordKind(keywordKind);
        }

        public static string Pluralize(this string word) => Pluralizer.Pluralize(word);
    }
}