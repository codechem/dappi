using System;
using System.Text;
using System.Text.RegularExpressions;

namespace Dappi.Core.Utils
{
    public static class CamelCaseConverter
    {
        public static string ToCamelCase(this string text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return text;
            }

            var parts = Regex.Split(text.Trim(), @"[\s_\-]+");

            if (parts.Length == 0)
            {
                return text;
            }

            var sb = new StringBuilder();

            for (int i = 0; i < parts.Length; i++)
            {
                var part = parts[i];

                if (string.IsNullOrWhiteSpace(part))
                {
                    continue;
                }

                sb.Append(i == 0 ? ToLowerFirst(part) : ToUpperFirst(part));
            }

            return sb.ToString();
        }

        private static string ToLowerFirst(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return value;
            }

            if (value.Length == 1)
            {
                return value.ToLowerInvariant();
            }

            return char.ToLowerInvariant(value[0]) + value.Substring(1);
        }

        private static string ToUpperFirst(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return value;
            }

            if (value.Length == 1)
            {
                return value.ToUpperInvariant();
            }

            return char.ToUpperInvariant(value[0]) + value.Substring(1).ToLowerInvariant();
        }
    }
}
