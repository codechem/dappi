using Dappi.HeadlessCms.Models;

namespace Dappi.HeadlessCms.Tests.TestData
{
    public class ValidPropertyTypes : TheoryData<string>
    {
        public ValidPropertyTypes()
        {
            Add(nameof(MediaInfo));
            Add("DateTime");
            Add("DateOnly");
            Add("string");
            Add("int");
            Add("bool");
            Add("double");
            Add("float");
        }
    }

    public class InvalidPropertyTypesAndClassNames : TheoryData<string>
    {
        public InvalidPropertyTypesAndClassNames()
        {
            Add("123InvalidClass");
            Add("Invalid Name");
            Add("Invalid@ClassName");
            Add("class");
            Add("int");
            Add("public");
            Add("void");
            Add("class!");
            Add("Property@");
            Add("var");
            Add("await");
            Add("async");
        }
    }

    public class TestFieldTypeAndFieldName : TheoryData<string, string>
    {
        public TestFieldTypeAndFieldName()
        {
            Add("TestStringType", "string");
            Add("TestIntType", "int");
            Add("TestBoolType", "bool");
            Add("TestDateTimeType", "DateTime");
            Add("TestDateOnlyType", "DateOnly");
        }
    }

    public class ValidMinMaxConstraints : TheoryData<string, double?, double?, string>
    {
        public ValidMinMaxConstraints()
        {
            Add("string", 0, 10, "StringMinMax");
            Add("string", 0, null, "StringMinOnly");
            Add("string", null, 50, "StringMaxOnly");

            Add("int", -10, 10, "IntNegative");
            Add("int", 0, 100, "IntMinMax");
            Add("double", -99.5, 99.5, "DoubleMinMax");
            Add("float", 0.5, 100.5, "FloatMinMax");

            Add("int", 5, 5, "IntEqual");
        }
    }

    public class InvalidMinMaxConstraints : TheoryData<string, double?, double?, string>
    {
        public InvalidMinMaxConstraints()
        {
            Add("string", -1, 10, "StringNegativeMin");
            Add("string", 5.5, 10, "StringDecimalMin");
            Add("string", 1, 10.5, "StringDecimalMax");
            Add("string", -5, -1, "StringBothNegative");

            Add("int", 10, 5, "IntMinGreaterThanMax");
            Add("string", 50, 10, "StringMinGreaterThanMax");
            Add("double", 100.5, 50.5, "DoubleMinGreaterThanMax");
            Add("float", 100.5, 50.5, "FloatMinGreaterThanMax");
        }
    }
}
