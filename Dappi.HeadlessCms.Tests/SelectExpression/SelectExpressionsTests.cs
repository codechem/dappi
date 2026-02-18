using Dappi.Core.Extensions;

namespace Dappi.HeadlessCms.Tests.DataShaping;

public class SelectExpressionsTests
{
    private static readonly string[] PublicPropertyNames = ["CreatedAt", "Id", "IsDeleted", "Name"];

    [Fact]
    public void BuildSelectExpression_Should_Return_Null_When_Fields_Is_Null()
    {
        var nullStr = (string?)null;
        var expression = nullStr.BuildSelectExpression(PublicPropertyNames);

        Assert.Null(expression);
    }

    [Fact]
    public void BuildSelectExpression_Should_Return_Null_When_Fields_Is_Whitespace()
    {
        var emptyStr = string.Empty;
        var expression = emptyStr.BuildSelectExpression(PublicPropertyNames);

        Assert.Null(expression);
    }

    [Fact]
    public void BuildSelectExpression_Should_Return_Expression_For_Valid_Fields()
    {
        var validFields = "Id,Name";
        var expression = validFields.BuildSelectExpression(PublicPropertyNames);

        Assert.Equal("new (Id, Name)", expression);
    }

    [Fact]
    public void BuildSelectExpression_Should_Return_Expression_CaseInsensitive()
    {
        var validCaseInsensitiveFields = "id,nAMe";
        var expression = validCaseInsensitiveFields.BuildSelectExpression(PublicPropertyNames);

        Assert.Equal("new (Id, Name)", expression);
    }

    [Fact]
    public void BuildSelectExpression_Should_Trim_And_Remove_Empty_Entries()
    {
        var emptyEntriesString = "  Id ,  Name  , ,   ";
        var expression = emptyEntriesString.BuildSelectExpression(PublicPropertyNames);

        Assert.Equal("new (Id, Name)", expression);
    }

    [Fact]
    public void BuildSelectExpression_Should_Remove_Duplicates()
    {
        var duplicatesStr = "Id,Name,id,NAME,Id";
        var expression = duplicatesStr.BuildSelectExpression(PublicPropertyNames);

        Assert.Equal("new (Id, Name)", expression);
    }

    [Fact]
    public void BuildSelectExpression_Should_Throw_When_Field_Does_Not_Exist()
    {
        var unknownFieldStr = "Id,UnknownField";
        Assert.Throws<NotSupportedException>(() => unknownFieldStr.BuildSelectExpression(PublicPropertyNames));
    }

    [Fact]
    public void BuildSelectExpression_Should_Return_Null_When_No_Field_Is_Provided()
    {
        var noFieldStr = ",,,";
        var expression = noFieldStr.BuildSelectExpression(PublicPropertyNames);

        Assert.Null(expression);
    }
}

