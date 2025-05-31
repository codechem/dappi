namespace Dappi.HeadlessCms.Core
{
    public class DomainModelEntityInfo
    {
        public required string Name { get; set; }
        public required string Namespace { get; set; }
        public List<PropertyInfo> Properties { get; set; } = [];
    }

    public class PropertyInfo
    {
        public required string Name { get; set; }
        
        public required string Type { get; set; }
    }
}