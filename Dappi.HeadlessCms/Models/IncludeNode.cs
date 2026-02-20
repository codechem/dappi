namespace Dappi.HeadlessCms.Models
{
    public class IncludeNode
    {
        public IncludeNode(string name)
        {
            Name = name;
        }

        public string Name { get; }

        public IDictionary<string, IncludeNode> Children { get; } =
            new Dictionary<string, IncludeNode>(StringComparer.OrdinalIgnoreCase);
    }
}
