namespace Dappi.HeadlessCms.Core.Extensions
{
    public static class TypeExtensions
    {
        private static Dictionary<string, string> _intrinsicTypes = new()
        {
            { "Boolean", "bool" },
            { "Byte", "byte" },
            { "SByte", "sbyte" },
            { "Int16", "short" },
            { "UInt16", "ushort" },
            { "Int32", "int" },
            { "UInt32", "uint" },
            { "Int64", "long" },
            { "UInt64", "ulong" },
            { "Single", "float" },
            { "Double", "double" },
            { "Decimal", "decimal" },
            { "Char", "char" },
            { "String", "string" },
        };

        public static string GetDisplayName(this Type t)
        {
            string res;
            if (t.IsGenericType && t.GetGenericTypeDefinition() == typeof(Nullable<>))
            {
                res = $"{GetDisplayName(t.GetGenericArguments()[0])}";
            }
            else if (t.IsGenericType)
            {
                res = string.Format("{0}<{1}>",
                    t.Name.Remove(t.Name.IndexOf('`')),
                    string.Join(",", t.GetGenericArguments().Select(at => at.GetDisplayName())));
            }
            else if (t.IsArray)
            {
                res = string.Format("{0}[{1}]",
                    GetDisplayName(t.GetElementType()),
                    new string(',', t.GetArrayRank() - 1));
            }
            else
            {
                res = t.Name;
            }
            if (_intrinsicTypes.TryGetValue(res, out var mapped))
            {
                res = mapped;
            }
            return res;
        }
    }
}