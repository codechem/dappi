using Dappi.HeadlessCms.Enums;

namespace Dappi.HeadlessCms.Models
{
    public class FilterRequest
    {
        public Dictionary<string,Dictionary<string, Dictionary<string, string>>>? Filters { get; set; }
    }

    public class Filter
    {
        public List<string>? Fields { get; set; } = [];
        public Operator Operator { get; set; } = Operator.Or;
        public Operation Operation { get; set; }
        public string Value { get; set; } = "";
    }
    
    public class FilterQuery
    {
        public List<ConditionGroup> Filters { get; set; } = new List<ConditionGroup>();
    }

    public class ConditionGroup
    {
        public List<Condition> AndConditions { get; set; } = new List<Condition>();
        public List<Condition> OrConditions { get; set; } = new List<Condition>();
    }

    public class Condition
    {
        public string Name { get; set; }
        public object Value { get; set; }
        public string Operator { get; set; }  // $eq, $gte, etc.
    }
}