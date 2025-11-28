using System.Dynamic;
using System.Reflection;
using Dappi.HeadlessCms.Exceptions;
using Dappi.HeadlessCms.Interfaces;

namespace Dappi.HeadlessCms.Services
{
    public class DataShaper : IDataShaper, IDisposable
    {
        private const BindingFlags BindingFlags = System.Reflection.BindingFlags.IgnoreCase |
                                                  System.Reflection.BindingFlags.Public |
                                                  System.Reflection.BindingFlags.Instance;

        private readonly IDictionary<string, List<PropertyInfo>> _propertyInfoCache =
            new Dictionary<string, List<PropertyInfo>>();

        public ExpandoObject ShapeObject<TSource>(TSource source, string? fields)
        {
            ArgumentNullException.ThrowIfNull(source);

            var dataShapedObject = new ExpandoObject();

            if (!_propertyInfoCache.TryGetValue(source.GetType().Name, out var value) || value.Count == 0)
            {
                CacheProperties<TSource>(fields);
            }

            foreach (var propertyInfo in _propertyInfoCache[source.GetType().Name])
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
                _propertyInfoCache.Add(typeof(TSource).Name, propertyInfos.ToList());
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

                    if (_propertyInfoCache.ContainsKey(typeof(TSource).Name))
                    {
                        _propertyInfoCache[typeof(TSource).Name].Add(propertyInfo);
                    }
                    else
                    {
                        _propertyInfoCache.Add(typeof(TSource).Name, [propertyInfo]);
                    }
                }
            }
        }

        public void Dispose()
        {
            _propertyInfoCache.Clear();
        }
    }
}