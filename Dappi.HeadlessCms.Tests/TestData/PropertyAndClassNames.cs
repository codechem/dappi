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
}