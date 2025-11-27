using System.Dynamic;
using System.Reflection;
using Dappi.HeadlessCms.Exceptions;

namespace Dappi.HeadlessCms.Extensions
{
    public static class ObjectExtensions
    {
        public static ExpandoObject ShapeObject<TSource>(this TSource source, string? fields)
        {
            ArgumentNullException.ThrowIfNull(source);

            var dataShapedObject = new ExpandoObject();

            if (string.IsNullOrEmpty(fields))
            {
                var propertyInfos = typeof(TSource).GetProperties(BindingFlags.IgnoreCase
                                                                  | BindingFlags.Public
                                                                  | BindingFlags.Instance);
                foreach (var propertyInfo in propertyInfos)
                {
                    var propertyValue = propertyInfo.GetValue(source);
                    ((IDictionary<string, object?>)dataShapedObject).Add(propertyInfo.Name, propertyValue);
                }
                return dataShapedObject;
            }

            foreach (var field in fields.Split(','))
            {
                var propName = field.Trim();
                var propertyInfo = typeof(TSource).GetProperty(propName,
                    BindingFlags.IgnoreCase
                    | BindingFlags.Public
                    | BindingFlags.Instance);
                if (propertyInfo is null)
                {
                    throw new PropertyNotFoundException($"Property {propName} not found in {typeof(TSource).FullName}");
                }

                var propertyValue = propertyInfo.GetValue(source);
                ((IDictionary<string, object?>)dataShapedObject).Add(propertyInfo.Name, propertyValue);
            }

            return dataShapedObject;
        }
    }
}