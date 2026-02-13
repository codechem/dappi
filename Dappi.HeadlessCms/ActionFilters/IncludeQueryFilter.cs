using Dappi.HeadlessCms.Models;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Dappi.HeadlessCms.ActionFilters
{
    public class IncludeQueryFilter : ActionFilterAttribute
    {
        public const string IncludeParamsKey = "Includes";

        public override void OnActionExecuting(ActionExecutingContext context)
        {
            if (!context.HttpContext.Request.Query.TryGetValue("include", out var includeValues))
                return;

            var includeTree = new Dictionary<string, IncludeNode>(StringComparer.OrdinalIgnoreCase);

            foreach (var segments in from includeValue in includeValues.OfType<string>() select includeValue
                         .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries) into includePaths from includePath in includePaths select includePath
                         .Split('.', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                         .ToArray() into segments where segments.Length != 0 select segments)
            {
                AddSegments(includeTree, segments, 0);
            }

            if (includeTree.Count == 0)
            {
                return;
            }

            context.HttpContext.Items[IncludeParamsKey] = includeTree;
        }

        private static void AddSegments(IDictionary<string, IncludeNode> nodes, IReadOnlyList<string> segments, int index)
        {
            while (index != segments.Count)
            {
                var segment = CapitalizeSegment(segments[index]);
                if (!nodes.TryGetValue(segment, out var current))
                {
                    current = new IncludeNode(segment);
                    nodes[segment] = current;
                }

                nodes = current.Children;
                index++;
            }
        }

        private static string CapitalizeSegment(string segment)
        {
            if (string.IsNullOrEmpty(segment))
            {
                return segment;
            }

            if (segment.Length == 1)
            {
                return segment.ToUpperInvariant();
            }

            return char.ToUpperInvariant(segment[0]) + segment.Substring(1);
        }
    }
}
