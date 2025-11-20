using Dappi.HeadlessCms.Enums;

namespace Dappi.HeadlessCms.Models
{
    public class Filter
    {
        public List<string>? Fields { get; set; } = [];
        public Operator Operator { get; set; } = Operator.Or;
        public Operation Operation { get; set; }
        public string Value { get; set; } = "";
    }
}