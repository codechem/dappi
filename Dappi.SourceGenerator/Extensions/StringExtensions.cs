using Pluralize.NET;

namespace Dappi.SourceGenerator.Extensions
{
    public static class StringExtensions
    {
        static readonly IPluralize Pluralizer = new Pluralizer();
        
        public static string Pluralize(this string word) => Pluralizer.Pluralize(word);
    }
}