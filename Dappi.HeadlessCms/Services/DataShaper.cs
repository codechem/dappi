using System.Dynamic;
using System.Reflection;
using Dappi.HeadlessCms.Exceptions;
using Dappi.HeadlessCms.Interfaces;

namespace Dappi.HeadlessCms.Services
{
    public class DataShaper : IDataShaper , IDisposable
    {
        private const BindingFlags BindingFlags = System.Reflection.BindingFlags.IgnoreCase |
                                                  System.Reflection.BindingFlags.Public |
                                                  System.Reflection.BindingFlags.Instance;

        private readonly List<PropertyInfo> _propertyInfoCache = [];

        public ExpandoObject ShapeObject<TSource>(TSource source, string? fields)
        {
            ArgumentNullException.ThrowIfNull(source);

            var dataShapedObject = new ExpandoObject();

            if (_propertyInfoCache.Count == 0)
            {
                CacheProperties<TSource>(fields);
            }

            foreach (var propertyInfo in _propertyInfoCache)
            {
                var propertyValue = propertyInfo.GetValue(source);
                ((IDictionary<string, object?>)dataShapedObject).Add(propertyInfo.Name, propertyValue);
            }

            return dataShapedObject;
        }

        private void CacheProperties<TSource>(string? fields)
        {
            if (string.IsNullOrEmpty(fields))
            {
                var propertyInfos = typeof(TSource).GetProperties(BindingFlags);
                _propertyInfoCache.AddRange(propertyInfos);
            }
            else
            {
                foreach (var field in fields.Split(','))
                {
                    var propertyName = field.Trim();
                    var propertyInfo = typeof(TSource).GetProperty(propertyName, BindingFlags);
                    if (propertyInfo is null)
                    {
                        throw new PropertyNotFoundException(
                            $"Property {propertyName} not found in {typeof(TSource).FullName}");
                    }

                    _propertyInfoCache.Add(propertyInfo);
                }
            }
        }

        public void Dispose()
        {
            _propertyInfoCache.Clear();
        }
    }
}