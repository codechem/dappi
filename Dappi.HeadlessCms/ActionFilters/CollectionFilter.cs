using System.Text.RegularExpressions;
using Dappi.HeadlessCms.Enums;
using Dappi.HeadlessCms.Models;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Dappi.HeadlessCms.ActionFilters
{
    public class CollectionFilter : ActionFilterAttribute
    {
        public const string FilterParamsKey = "FilterParams";
        
        public override void OnActionExecuting(ActionExecutingContext context)
        {
            var filters = context.HttpContext.Request.Query
                .Where(x =>
                    x.Key.Contains("filter", StringComparison.CurrentCultureIgnoreCase)
                ).ToList();
            
            var filterList = new List<Filter>();

            foreach (var filter in filters)
            {
                const string pattern = @"\[(.*?)\]";

                var matches = Regex.Matches(filter.Key, pattern);
                const string dollar = "$";
                var newFilter = new Filter
                {
                    Value = filter.Value.ToString(),
                };
                foreach (Match match in matches)
                {
                    var value = match.Value.Replace("[", string.Empty).Replace("]", "");
                    if (value.Equals("$and") || value.Equals("$or"))
                    {
                        value = value.Replace(dollar, string.Empty);
                        var parsedOperator = Enum.Parse<Operator>(value, true);
                        newFilter.Operator = parsedOperator;
                    } 
                    else if (value.StartsWith(dollar) && (!value.Equals("$and") || !value.Equals("$or")))
                    {
                        value = value.Replace(dollar, string.Empty);
                        if (Enum.TryParse(value, true, out Operation operation))
                        {
                            newFilter.Operation = operation;
                        }
                        else
                        {
                            throw new ArgumentException($"Invalid filter operation: {value}");
                        }
                    }
                    else if (int.TryParse(value, out int parsedValue))
                    {
                        //Logic for indexing TBD
                    }
                    else
                    {
                        newFilter.Fields.Add(value);
                    }
                }

                filterList.Add(newFilter);
            }
            context.HttpContext.Items[FilterParamsKey] = filterList;
        }
    }
}