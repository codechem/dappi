using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace Dappi.Shared.Utils
{
    public static class Pluralizer
    {
        private static readonly Dictionary<string, string> Irregular = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            {"person", "people"},
            {"man", "men"},
            {"woman", "women"},
            {"child", "children"},
            {"tooth", "teeth"},
            {"foot", "feet"},
            {"mouse", "mice"},
            {"goose", "geese"},
            {"ox", "oxen"},
            {"louse", "lice"},
            {"die", "dice"},
            {"index", "indices"},
            {"appendix", "appendices"},
            {"cactus", "cacti"},
            {"focus", "foci"},
            {"fungus", "fungi"},
            {"nucleus", "nuclei"},
            {"radius", "radii"},
            {"stimulus", "stimuli"},
            {"analysis", "analyses"},
            {"thesis", "theses"},
            {"crisis", "crises"},
            {"phenomenon", "phenomena"},
            {"criterion", "criteria"},
            {"datum", "data"}
        };

        private static readonly HashSet<string> Uncountables = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "sheep", "fish", "deer", "series", "species", "money", "rice", "information",
            "equipment", "knowledge", "traffic", "baggage", "furniture", "advice"
        };

        private static readonly HashSet<string> F_Exceptions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "roof", "belief", "chef", "chief", "proof", "safe"
        };

        private static readonly HashSet<string> O_Es_Exceptions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "hero", "echo", "potato", "tomato", "torpedo", "veto"
        };

        public static string Pluralize(this string word)
        {
            if (string.IsNullOrWhiteSpace(word))
            {
                return word;
            }

            var lower = word.ToLowerInvariant();


            // Uncountables
            if (Uncountables.Contains(lower))
            {
                return word;
            }

            // Irregular
            if (Irregular.TryGetValue(lower, out var irregular))
            {
                return MatchCase(word, irregular);
            }

            // Common endings: s, x, z, ch, sh → +es
            if (Regex.IsMatch(lower, "(s|x|z|ch|sh)$"))
            {
                return MatchCase(word, word + "es");
            }

            // Ends with consonant + y → -ies
            if (Regex.IsMatch(lower, "[^aeiou]y$"))
            {
                return MatchCase(word, word.TakeCharsFromEnd(1) + "ies");
            }

            // Ends with vowel + y → +s
            if (lower.EndsWith("y"))
            {
                return MatchCase(word, word + "s");
            }

            // Ends with -fe or -f → -ves (except some)
            if (lower.EndsWith("fe"))
            {
                if (F_Exceptions.Contains(lower))
                {
                    return MatchCase(word, word + "s");
                }
                return MatchCase(word, word.TakeCharsFromEnd(2) + "ves");
            }
            if (lower.EndsWith("f"))
            {
                if (F_Exceptions.Contains(lower))
                {
                    return MatchCase(word, word + "s");
                }
                return MatchCase(word, word.TakeCharsFromEnd(1) + "ves");
            }

            // Ends with -o → +es for exceptions
            if (lower.EndsWith("o"))
            {
                if (O_Es_Exceptions.Contains(lower))
                {
                    return MatchCase(word, word + "es");
                }
                return MatchCase(word, word + "s");
            }

            // Ends with -is → -es
            if (lower.EndsWith("is"))
            {
                return MatchCase(word, word.TakeCharsFromEnd(2) + "es");
            }

            // Default: add 's'
            return MatchCase(word, word + "s");
        }

        private static string MatchCase(string original, string result)
        {
            if (string.IsNullOrEmpty(original))
            {
                return result;
            }

            if (IsAllUpper(original))
            {
                return result.ToUpperInvariant();
            }

            if (char.IsUpper(original[0]) && original.Length > 1 && original[1].IsLower())
            {
                return char.ToUpper(result[0]) + result.Substring(1);
            }

            return result;
        }

        private static string TakeCharsFromEnd(this string text, int numChars)
        {
            return text.Substring(0, text.Length - numChars);
        }

        private static bool IsAllUpper(string s)
        {
            foreach (var c in s)
            {
                if (char.IsLetter(c) && char.IsLower(c))
                {
                    return false;
                }
            }
            return true;
        }

        private static bool IsLower(this char c) => char.IsLower(c);
    }
}
