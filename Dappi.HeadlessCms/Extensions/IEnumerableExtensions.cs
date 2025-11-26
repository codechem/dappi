using System.Dynamic;
using System.Reflection;

namespace Dappi.HeadlessCms.Extensions
{
    public static class IEnumerableExtensions
    {
        public static IEnumerable<ExpandoObject> ShapeData<TSource>(this IEnumerable<TSource> source, string? fields)
        {
            ArgumentNullException.ThrowIfNull(source);
            var objectList = new List<ExpandoObject>();
            var propertyInfoList = new List<PropertyInfo>();
            if (string.IsNullOrEmpty(fields))
            {
                var propertyInfos =
                    typeof(TSource).GetProperties(BindingFlags.IgnoreCase |
                                                  BindingFlags.Public |
                                                  BindingFlags.Instance);
                propertyInfoList.AddRange(propertyInfos);
            }
            else
            {
                foreach (var field in fields.Split(','))
                {
                    var propertyName = field.Trim();
                    var propertyInfo = typeof(TSource).GetProperty(propertyName,
                        BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance);
                    if (propertyInfo is null)
                    {
                        throw new Exception($"Property {propertyName} not found in {typeof(TSource).FullName}");
                    }

                    propertyInfoList.Add(propertyInfo);
                }
            }

            foreach (var sourceObject in source)
            {
                var expandoObject = new ExpandoObject();
                foreach (var propertyInfo in propertyInfoList)
                {
                    var propertyValue = propertyInfo.GetValue(sourceObject);
                    ((IDictionary<string, object?>)expandoObject).Add(propertyInfo.Name, propertyValue);
                }
                objectList.Add(expandoObject);
            }

            return objectList;
        }
    }
}