using Dappi.HeadlessCms.Exceptions;

namespace Dappi.HeadlessCms.Tests.DataShaping;

public class SelectExpressionsTests
{
    private static readonly string[] PublicPropertyNames = ["CreatedAt", "Id", "IsDeleted", "Name"];

    [Fact]
    public void BuildSelectExpression_Should_Return_Null_When_Fields_Is_Null()
    {
        var expression = BuildSelectExpression(null);

        Assert.Null(expression);
    }

    [Fact]
    public void BuildSelectExpression_Should_Return_Null_When_Fields_Is_Whitespace()
    {
        var expression = BuildSelectExpression("   ");

        Assert.Null(expression);
    }

    [Fact]
    public void BuildSelectExpression_Should_Return_Expression_For_Valid_Fields()
    {
        var expression = BuildSelectExpression("Id,Name");

        Assert.Equal("new (Id, Name)", expression);
    }

    [Fact]
    public void BuildSelectExpression_Should_Return_Expression_CaseInsensitive()
    {
        var expression = BuildSelectExpression("id,nAMe");

        Assert.Equal("new (Id, Name)", expression);
    }

    [Fact]
    public void BuildSelectExpression_Should_Trim_And_Remove_Empty_Entries()
    {
        var expression = BuildSelectExpression("  Id ,  Name  , ,   ");

        Assert.Equal("new (Id, Name)", expression);
    }

    [Fact]
    public void BuildSelectExpression_Should_Remove_Duplicates()
    {
        var expression = BuildSelectExpression("Id,Name,id,NAME,Id");

        Assert.Equal("new (Id, Name)", expression);
    }

    [Fact]
    public void BuildSelectExpression_Should_Throw_When_Field_Does_Not_Exist()
    {
        Assert.Throws<PropertyNotFoundException>(() => BuildSelectExpression("Id,UnknownField"));
    }

    [Fact]
    public void BuildSelectExpression_Should_Return_Null_When_No_Field_Is_Provided()
    {
        var expression = BuildSelectExpression(",,,");

        Assert.Null(expression);
    }

    private sealed class DataShapingDummyModel
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public bool IsDeleted { get; set; }
    }
    
    private static string? BuildSelectExpression(string? fields)
    {
        if (string.IsNullOrWhiteSpace(fields))
        {
            return null;
        }

        var propertyMap = PublicPropertyNames
            .ToDictionary(property => property, property => property, StringComparer.OrdinalIgnoreCase);

        var requestedFields = fields
            .Split(',', StringSplitOptions.RemoveEmptyEntries)
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
                throw new PropertyNotFoundException(
                    "Property " + field + " not found in " + typeof(DataShapingDummyModel).FullName);
            }

            if (selected.Add(propertyName))
            {
                selectParts.Add(propertyName);
            }
        }

        return "new (" + string.Join(", ", selectParts) + ")";
    }
}
