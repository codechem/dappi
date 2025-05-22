namespace CCApi.Extensions.DependencyInjection.Models
{
    public class FieldsInfo
    {
        [System.Text.Json.Serialization.JsonPropertyName("fieldName")]
        public string FieldName { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("fieldType")]
        public string FieldType { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("isRequired")]
        public bool IsRequired { get; set; }
        [System.Text.Json.Serialization.JsonPropertyName("relatedTo")]
        public bool RelatedTo { get; set; }
    }

}