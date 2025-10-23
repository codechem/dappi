using System.Reflection;

namespace Dappi.HeadlessCms.Core.Extensions
{
    public static class PropertyInfoExtensions
    {
        public static bool IsNullable(this PropertyInfo property)
        {
            if (Nullable.GetUnderlyingType(property.PropertyType) != null)
            {
                return true; 
            }
            return !property.PropertyType.IsValueType;
        }
    }
}