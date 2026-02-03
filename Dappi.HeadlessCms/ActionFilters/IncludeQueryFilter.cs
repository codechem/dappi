using Dappi.HeadlessCms.Models;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Dappi.HeadlessCms.ActionFilters
{
    public class IncludeQueryFilter : ActionFilterAttribute
    {
        private const string IncludeParamsKey = "Includes";

        public override void OnActionExecuting(ActionExecutingContext context)
        {
            if (!context.HttpContext.Request.Query.TryGetValue("include", out var includeValues))
                return;

            var includeTree = new Dictionary<string, IncludeNode>(StringComparer.OrdinalIgnoreCase);

            foreach (var includeValue in includeValues)
            {
                if (includeValue is null)
                    continue;

                var includePaths = includeValue
                    .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

                foreach (var includePath in includePaths)
                {
                    var segments = includePath
                        .Split('.', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                        .ToArray();

                    if (segments.Length == 0)
                        continue;

                    AddSegmentsRecursive(includeTree, segments, 0);
                }
            }

            if (includeTree.Count == 0)
            {
                return;
            }

            context.HttpContext.Items[IncludeParamsKey] = includeTree;
        }

        private static void AddSegmentsRecursive(IDictionary<string, IncludeNode> nodes, IReadOnlyList<string> segments, int index)
        {
            while (index != segments.Count)
            {
                var segment = segments[index];
                if (!nodes.TryGetValue(segment, out var current))
                {
                    current = new IncludeNode(segment);
                    nodes[segment] = current;
                }

                nodes = current.Children;
                index++;
            }
        }
    }
}
