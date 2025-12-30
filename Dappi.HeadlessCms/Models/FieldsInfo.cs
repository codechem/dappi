namespace Dappi.HeadlessCms.Models
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
        
        [System.Text.Json.Serialization.JsonPropertyName("regex")]
        public string? Regex { get; set; }
        
        [System.Text.Json.Serialization.JsonPropertyName("hasIndex")]
        public bool HasIndex { get; set; }
    }
}