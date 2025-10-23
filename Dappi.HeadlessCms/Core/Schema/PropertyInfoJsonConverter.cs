using System.Text.Json;
using System.Text.Json.Serialization;

namespace Dappi.HeadlessCms.Core.Schema
{
    public class PropertyInfoJsonConverter : JsonConverter<DappiPropertyInfo>
    {
        public override DappiPropertyInfo? Read(ref Utf8JsonReader reader, Type typeToConvert,
            JsonSerializerOptions options)
        {
            throw new NotImplementedException("Deserialization is not implemented for this schema.");
        }

        public override void Write(Utf8JsonWriter writer, DappiPropertyInfo value, JsonSerializerOptions options)
        {
            var type = value.GetType();
            var dict = new Dictionary<string, object?> { ["type"] = value.Type, ["required"] = value.Required };

            foreach (var prop in type.GetProperties().Where(p => p.DeclaringType != typeof(DappiPropertyInfo)))
            {
                dict[prop.Name.ToCamelCase()] = prop.GetValue(value);
            }

            JsonSerializer.Serialize(writer, dict, options);
        }
    }
}