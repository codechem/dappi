using Dappi.HeadlessCms.Models;

namespace Dappi.HeadlessCms.Tests.Core
{
    public class PropertyTestData : TheoryData<string>
    {
        public PropertyTestData()
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
}