using Dappi.Core.Utils;

namespace Dappi.Core.Tests.Pluralizer;

public class PluralizerTests
{

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public void Null_Or_Empty_Return_Original_Input(string? word)
    {
        var actual = word.Pluralize();
        Assert.Equal(word, actual);
    }

    [Theory]
    [ClassData(typeof(UncountablesTestData))]
    public void Uncountables_Stay_Same(string word)
    {
        var actual = word.Pluralize();
        Assert.Equal(word, actual);
    }

    [Theory]
    [ClassData(typeof(IrregularWordsTestData))]
    public void Irregular_Words_Match(string word, string expected)
    {
        var actual = word.Pluralize();
        Assert.Equal(expected, actual);
    }

    [Theory]
    [ClassData(typeof(IS_ES_TestData))]
    public void Words_Ending_With_IS_End_With_ES(string word, string expected)
    {
        var actual = word.Pluralize();
        Assert.Equal(expected, actual);
    }

    //Words ending with S, X , Z , CH , SH should end with ES
    [Theory]
    [ClassData(typeof(ES_TestData))]
    public void Word_Ends_With_ES(string word, string expected)
    {
        var actual = word.Pluralize();
        Assert.Equal(expected, actual);
    }

    [Theory]
    [ClassData(typeof(Y_IES_TestData))]
    public void Word_Ending_With_ConstantY_Ends_With_IES(string word, string expected)
    {
        var actual = word.Pluralize();
        Assert.Equal(expected, actual);
    }

    [Theory]
    [ClassData(typeof(Y_S_TestData))]
    public void Words_Ending_With_Y_Should_End_With_S(string word, string expected)
    {
        var actual = word.Pluralize();
        Assert.Equal(expected, actual);
    }

    [Theory]
    [ClassData(typeof(F_ExceptionsTestData))]
    public void F_Exceptions_End_With_S(string word, string expected)
    {
        var actual = word.Pluralize();
        Assert.Equal(expected, actual);
    }

    [Theory]
    [ClassData(typeof(F_FE_VES_TestData))]
    public void Words_Ending_With_F_FE_Should_End_With_VES(string word, string expected)
    {
        var actual = word.Pluralize();
        Assert.Equal(expected, actual);
    }

    [Theory]
    [ClassData(typeof(O_ExceptionsTestData))]
    public void O_Exceptions_End_With_ES(string word, string expected)
    {
        var actual = word.Pluralize();
        Assert.Equal(expected, actual);
    }

    [Theory]
    [ClassData(typeof(O_TestData))]
    public void Words_Ending_With_O_End_With_S(string word, string expected)
    {
        var actual = word.Pluralize();
        Assert.Equal(expected, actual);
    }

    [Theory]
    [ClassData(typeof(S_TestData))]
    public void Words_End_With_S(string word, string expected)
    {
        var actual = word.Pluralize();
        Assert.Equal(expected, actual);
    }

    [Theory]
    [ClassData(typeof(Maintain_Case_TestData))]
    public void Word_Maintains_Case(string word, string expected)
    {
        var actual = word.Pluralize();
        Assert.Equal(actual, expected);
    }
}
