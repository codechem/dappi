namespace Dappi.SourceGenerator.Models
{
    public class EnumInfo
    {
        public string Name { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Namespace { get; set; } = string.Empty;
        public EnumMember[] Members { get; set; } = Array.Empty<EnumMember>();
    }
}