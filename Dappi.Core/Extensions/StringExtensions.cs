namespace Dappi.Core.Extensions;

public static class StringExtensions
{

    public static string? BuildSelectExpression(
        this string? fields,
        IEnumerable<string> publicPropertyNames)
    {
        if (string.IsNullOrWhiteSpace(fields))
        {
            return null;
        }

        if (publicPropertyNames is null)
        {
            throw new ArgumentNullException(nameof(publicPropertyNames));
        }

        var propertyMap = publicPropertyNames
            .ToDictionary(property => property, property => property, StringComparer.OrdinalIgnoreCase);

        var requestedFields = fields!
            .Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
            .Select(field => field.Trim())
            .Where(field => !string.IsNullOrWhiteSpace(field))
            .ToArray();

        if (requestedFields.Length == 0)
        {
            return null;
        }

        var selected = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var selectParts = new List<string>(requestedFields.Length);

        foreach (var field in requestedFields)
        {
            if (!propertyMap.TryGetValue(field, out var propertyName))
            {
                throw new NotSupportedException($"Property '{field}' can not be selected.");
            }

            if (selected.Add(propertyName))
            {
                selectParts.Add(propertyName);
            }
        }

        return "new (" + string.Join(", ", selectParts) + ")";
    }
}