namespace Dappi.HeadlessCms.Core.Extensions
{
    public static class TypeExtensions
    {
        public static string GetDisplayName(this Type t)
        {
            if (t.IsGenericType && t.GetGenericTypeDefinition() == typeof(Nullable<>))
                return $"{GetDisplayName(t.GetGenericArguments()[0])}";
            if (t.IsGenericType)
                return string.Format("{0}<{1}>",
                    t.Name.Remove(t.Name.IndexOf('`')),
                    string.Join(",", t.GetGenericArguments().Select(at => at.GetDisplayName())));
            if (t.IsArray)
                return string.Format("{0}[{1}]",
                    GetDisplayName(t.GetElementType()),
                    new string(',', t.GetArrayRank() - 1));
            return t.Name;
        }
    }
}